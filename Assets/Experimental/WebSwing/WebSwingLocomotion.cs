using HeroVR.Combat;
using HeroVR.XR;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.Experimental
{
    /// <summary>
    /// Pendulum web-swinging for the wall-crawler.
    ///
    /// EXPERIMENTAL / ADDITIVE. Lives outside Assets/Scripts and changes no gameplay-owned file.
    ///
    /// Why the motor is disabled rather than extended: XRCharacterMotor is sealed and its Update()
    /// recomputes velocity from scratch every frame before driving the CharacterController itself,
    /// so there is no seam to feed momentum through. This component takes the controller for the
    /// whole airborne phase and hands it back on landing, so the two never fight. The proper fix is
    /// an external-velocity API on XRCharacterMotor; that is a gameplay-side change.
    ///
    /// Aiming comes from the tracked controller transforms, not the physics hands. The physics
    /// hands lag behind the real pose and rotate under physics, so aiming from them made the web
    /// fire somewhere other than where the player was pointing. The web is still drawn from the
    /// physics hand, because that is the object the player actually sees.
    ///
    /// Input uses the grip buttons, which XRHeroInputAdapter leaves free.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class WebSwingLocomotion : MonoBehaviour
    {
        private enum State
        {
            Grounded,
            Swinging,
            Airborne
        }

        public enum AimMode
        {
            /// <summary>
            /// Point along the controller, corrected by <see cref="aimPitchOffset"/>. A Touch
            /// controller's forward axis runs down the handle, well below where the hand appears
            /// to point, so it needs tilting up to match player intent.
            /// </summary>
            ControllerForward = 0,

            /// <summary>
            /// Ray from the head through the hand and outward. Ignores controller orientation
            /// entirely, so wrist angle cannot throw the aim off. Usually the more intuitive of
            /// the two for distant targets.
            /// </summary>
            HeadThroughHand = 1
        }

        [Header("Aim sources (tracked controllers - accurate pose)")]
        [SerializeField] private Transform leftAim;
        [SerializeField] private Transform rightAim;
        [SerializeField] private Transform head;

        [Header("Web origins (physics hands - what the player sees)")]
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;

        [Header("Aiming")]
        [SerializeField] private AimMode aimMode = AimMode.ControllerForward;
        [SerializeField, Min(1f)] private float maxWebRange = 35f;
        [Tooltip("0 is a pure raycast. Small values forgive shaky hands without snapping to " +
                 "things you did not aim at. Above about 0.4 it starts to feel like autolock.")]
        [SerializeField, Range(0f, 1f)] private float aimAssistRadius = .18f;
        [SerializeField] private LayerMask anchorLayers = ~0;
        [Tooltip("ControllerForward only. Negative tilts the aim upward. A Touch controller's " +
                 "forward runs down the handle, so roughly -35 lines it up with where the hand " +
                 "looks like it is pointing. Raise toward 0 if webs fire too high.")]
        [SerializeField, Range(-60f, 30f)] private float aimPitchOffset = -35f;

        [Header("Swing feel")]
        [SerializeField] private float gravity = -20f;
        [Tooltip("How fast the rope shortens while held. Higher lifts you harder through the arc.")]
        [SerializeField, Min(0f)] private float reelInSpeed = 4.5f;
        [SerializeField, Min(1f)] private float minRopeLength = 2.5f;
        [Tooltip("Steering while swinging. Keep low or momentum stops mattering.")]
        [SerializeField, Min(0f)] private float airControl = 5f;
        [Tooltip("Push along your look direction the instant you let go.")]
        [SerializeField, Min(0f)] private float releaseBoost = 3.5f;
        [Tooltip("Speed kept when a swing starts, so attaching does not feel like a stop.")]
        [SerializeField, Min(0f)] private float attachSpeedCarry = 5f;
        [SerializeField, Min(0f)] private float maxSpeed = 30f;

        [Header("Web visual")]
        [SerializeField, Min(.005f)] private float webThickness = .022f;
        [Tooltip("How long a missed web stays visible before retracting.")]
        [SerializeField, Min(0f)] private float missVisualDuration = .18f;

        private CharacterController controller;
        private XRCharacterMotor motor;
        private Damageable health;

        private InputAction leftGrip;
        private InputAction rightGrip;
        private bool leftWasHeld;
        private bool rightWasHeld;

        private State state = State.Grounded;
        private Vector3 velocity;
        private Vector3 anchor;
        private float ropeLength;
        private Transform activeOrigin;

        private LineRenderer web;
        private Vector3 missEnd;
        private Transform missOrigin;
        private float missUntil;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            motor = GetComponent<XRCharacterMotor>();
            health = GetComponent<Damageable>();

            if (head == null && motor != null)
                head = motor.Head;

            leftGrip = new InputAction(
                "WebSwingLeft", InputActionType.Button, "<XRController>{LeftHand}/gripPressed");
            rightGrip = new InputAction(
                "WebSwingRight", InputActionType.Button, "<XRController>{RightHand}/gripPressed");

            CreateWebRenderer();
        }

        private void OnEnable()
        {
            leftGrip.Enable();
            rightGrip.Enable();
        }

        private void OnDisable()
        {
            leftGrip.Disable();
            rightGrip.Disable();
            ReleaseControl();
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                if (state != State.Grounded)
                    ReleaseControl();
                UpdateWebVisual();
                return;
            }

            bool leftHeld = leftGrip.IsPressed();
            bool rightHeld = rightGrip.IsPressed();

            // Fire on the press edge so every squeeze produces a visible shot, hit or miss.
            if (leftHeld && !leftWasHeld)
                FireWeb(leftAim, leftHand);
            if (rightHeld && !rightWasHeld)
                FireWeb(rightAim, rightHand);

            leftWasHeld = leftHeld;
            rightWasHeld = rightHeld;

            bool anyHeld = leftHeld || rightHeld;

            switch (state)
            {
                case State.Swinging:
                    if (!anyHeld)
                        Release();
                    else
                        TickSwing();
                    break;

                case State.Airborne:
                    TickAirborne();
                    break;
            }

            UpdateWebVisual();
        }

        /// <summary>
        /// Always produces a shot. On a hit it anchors; on a miss it still draws a web out to
        /// range and retracts, so a squeeze never feels like it was swallowed.
        /// </summary>
        private void FireWeb(Transform aim, Transform origin)
        {
            Transform aimSource = aim != null ? aim : head;
            if (aimSource == null)
                return;

            Transform visualOrigin = origin != null ? origin : aimSource;
            Vector3 direction = AimDirection(aimSource, visualOrigin);

            bool hitSomething = aimAssistRadius <= .001f
                ? Physics.Raycast(aimSource.position, direction, out RaycastHit hit,
                    maxWebRange, anchorLayers, QueryTriggerInteraction.Ignore)
                : Physics.SphereCast(aimSource.position, aimAssistRadius, direction, out hit,
                    maxWebRange, anchorLayers, QueryTriggerInteraction.Ignore);

            if (!hitSomething || hit.transform.root == transform.root)
            {
                ShowMiss(visualOrigin, aimSource.position + direction * maxWebRange);
                return;
            }

            anchor = hit.point;
            activeOrigin = visualOrigin;

            // Clamp rather than reject when the anchor is close. Rejecting made short-range
            // shots silently do nothing, which read as the web failing to fire.
            ropeLength = Mathf.Max(minRopeLength, Vector3.Distance(transform.position, anchor));

            if (state == State.Grounded)
            {
                Vector3 carry = motor != null ? motor.DesiredWorldMoveDirection : Vector3.zero;
                velocity = carry * attachSpeedCarry;
            }

            missUntil = 0f;
            TakeControl();
            state = State.Swinging;
        }

        private Vector3 AimDirection(Transform aimSource, Transform handVisual)
        {
            if (aimMode == AimMode.HeadThroughHand && head != null)
            {
                Transform handPoint = handVisual != null ? handVisual : aimSource;
                Vector3 through = handPoint.position - head.position;

                // Degenerate if the hand is at the face; fall back to the controller.
                if (through.sqrMagnitude > .0025f)
                    return through.normalized;
            }

            if (Mathf.Abs(aimPitchOffset) < .01f)
                return aimSource.forward;

            // Negative pitch tilts upward, correcting the Touch controller's handle-aligned axis.
            return Quaternion.AngleAxis(aimPitchOffset, aimSource.right) * aimSource.forward;
        }

        private void ShowMiss(Transform origin, Vector3 endPoint)
        {
            missOrigin = origin;
            missEnd = endPoint;
            missUntil = Time.time + missVisualDuration;
        }

        private void TickSwing()
        {
            float dt = Time.deltaTime;

            velocity += Vector3.up * gravity * dt;
            velocity += AirSteering() * (airControl * dt);

            ropeLength = Mathf.Max(minRopeLength, ropeLength - reelInSpeed * dt);

            Vector3 predicted = transform.position + velocity * dt;
            Vector3 toAnchor = anchor - predicted;
            float distance = toAnchor.magnitude;

            Vector3 correction = Vector3.zero;
            if (distance > ropeLength && distance > .001f)
            {
                Vector3 ropeDirection = toAnchor / distance;

                // A rope pulls but never pushes: cancel only outward radial velocity.
                float outward = Vector3.Dot(velocity, -ropeDirection);
                if (outward > 0f)
                    velocity += ropeDirection * outward;

                correction = ropeDirection * (distance - ropeLength);
            }

            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            controller.Move(velocity * dt + correction);

            if (controller.isGrounded && velocity.y <= 0f)
                ReleaseControl();
        }

        private void TickAirborne()
        {
            float dt = Time.deltaTime;

            velocity += Vector3.up * gravity * dt;
            velocity += AirSteering() * (airControl * dt);
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

            controller.Move(velocity * dt);

            if (controller.isGrounded && velocity.y <= 0f)
                ReleaseControl();
        }

        private void Release()
        {
            if (head != null)
                velocity += head.forward * releaseBoost;

            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            state = State.Airborne;
            activeOrigin = null;
        }

        private Vector3 AirSteering()
        {
            if (motor == null)
                return Vector3.zero;

            Vector3 steering = motor.DesiredWorldMoveDirection;
            steering.y = 0f;
            return steering;
        }

        private void TakeControl()
        {
            if (motor != null)
                motor.enabled = false;
        }

        private void ReleaseControl()
        {
            state = State.Grounded;
            activeOrigin = null;
            velocity = Vector3.zero;

            if (motor != null)
                motor.enabled = true;
        }

        private void CreateWebRenderer()
        {
            GameObject webObject = new GameObject("WebLine");
            webObject.transform.SetParent(transform, false);

            web = webObject.AddComponent<LineRenderer>();
            web.positionCount = 2;
            web.startWidth = webThickness;
            web.endWidth = webThickness;
            web.useWorldSpace = true;
            web.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            web.receiveShadows = false;
            web.enabled = false;

            // Unlit so the web reads against both bright sky and dark geometry.
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");

            web.material = new Material(shader) { color = Color.white };
        }

        private void UpdateWebVisual()
        {
            if (state == State.Swinging && activeOrigin != null)
            {
                web.enabled = true;
                web.SetPosition(0, activeOrigin.position);
                web.SetPosition(1, anchor);
                return;
            }

            if (Time.time < missUntil && missOrigin != null)
            {
                web.enabled = true;
                web.SetPosition(0, missOrigin.position);
                web.SetPosition(1, missEnd);
                return;
            }

            web.enabled = false;
        }

        private void OnValidate()
        {
            gravity = Mathf.Min(-.01f, gravity);
            minRopeLength = Mathf.Max(1f, minRopeLength);
            maxWebRange = Mathf.Max(minRopeLength, maxWebRange);
        }
    }
}
