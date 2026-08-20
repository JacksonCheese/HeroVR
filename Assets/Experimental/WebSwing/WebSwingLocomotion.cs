using HeroVR.Combat;
using HeroVR.XR;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.Experimental
{
    /// <summary>
    /// Pendulum web-swinging for the wall-crawler.
    ///
    /// EXPERIMENTAL / ADDITIVE. This deliberately lives outside Assets/Scripts and changes no
    /// gameplay-owned file.
    ///
    /// Why it works the way it does: XRCharacterMotor is sealed and its Update() recomputes
    /// velocity from scratch every frame
    ///     velocity = DesiredWorldMoveDirection * moveSpeed + up * verticalSpeed
    /// then drives the CharacterController itself. There is no seam to add momentum through, so
    /// any swing physics running alongside it would be overwritten each frame. Instead this
    /// component disables the motor for the whole airborne phase, drives the CharacterController
    /// directly, and re-enables the motor on landing. Two systems never move the controller at
    /// once.
    ///
    /// The proper long-term fix is an external-velocity API on XRCharacterMotor (something like
    /// AddExternalVelocity / SuspendLocomotion) so swinging is a normal contributor instead of a
    /// takeover. That is a gameplay-side change and is why this is marked experimental.
    ///
    /// Input uses the grip buttons, which the existing XRHeroInputAdapter leaves unbound - it uses
    /// the sticks, triggers, primary buttons, and stick click. Bindings are declared here rather
    /// than added to that adapter so no gameplay prefab or script needs editing.
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

        [Header("Hands")]
        [Tooltip("Anchor is aimed from these. Falls back to the head if left empty.")]
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform head;

        [Header("Web")]
        [SerializeField, Min(1f)] private float maxWebRange = 28f;
        [Tooltip("Spherecast radius. Wider is far more forgiving to aim in VR.")]
        [SerializeField, Min(.01f)] private float aimAssistRadius = .9f;
        [SerializeField] private LayerMask anchorLayers = ~0;

        [Header("Swing feel")]
        [SerializeField] private float gravity = -22f;
        [Tooltip("Rope shortens while held, which lifts you through the arc instead of just hanging.")]
        [SerializeField, Min(0f)] private float reelInSpeed = 3.5f;
        [SerializeField, Min(1f)] private float minRopeLength = 3f;
        [Tooltip("Steering while swinging or airborne. Keep low so momentum stays king.")]
        [SerializeField, Min(0f)] private float airControl = 4.5f;
        [Tooltip("Extra push along your look direction the moment you let go.")]
        [SerializeField, Min(0f)] private float releaseBoost = 2.5f;
        [SerializeField, Min(0f)] private float maxSpeed = 26f;

        [Header("Visuals")]
        [SerializeField] private Material webMaterial;
        [SerializeField, Min(.005f)] private float webThickness = .025f;

        private CharacterController controller;
        private XRCharacterMotor motor;
        private Damageable health;

        private InputAction leftGrip;
        private InputAction rightGrip;

        private State state = State.Grounded;
        private Vector3 velocity;
        private Vector3 anchor;
        private float ropeLength;
        private Transform activeHand;
        private LineRenderer web;

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

            // Never leave the player's normal locomotion switched off.
            ReleaseControl();
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                if (state != State.Grounded)
                    ReleaseControl();
                return;
            }

            bool leftHeld = leftGrip.IsPressed();
            bool rightHeld = rightGrip.IsPressed();
            bool anyHeld = leftHeld || rightHeld;

            switch (state)
            {
                case State.Grounded:
                    if (anyHeld)
                        TryAttach(rightHeld ? rightHand : leftHand);
                    break;

                case State.Swinging:
                    if (!anyHeld)
                        Release();
                    else
                        TickSwing();
                    break;

                case State.Airborne:
                    // Allow re-attaching mid-flight, which is what makes chained swings feel good.
                    if (anyHeld && TryAttach(rightHeld ? rightHand : leftHand))
                        break;
                    TickAirborne();
                    break;
            }

            UpdateWebVisual();
        }

        private bool TryAttach(Transform hand)
        {
            Transform aimSource = hand != null ? hand : head;
            if (aimSource == null)
                return false;

            // Ignore our own colliders so a web never sticks to the player.
            if (!Physics.SphereCast(
                    aimSource.position,
                    aimAssistRadius,
                    aimSource.forward,
                    out RaycastHit hit,
                    maxWebRange,
                    anchorLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.transform.root == transform.root)
                return false;

            anchor = hit.point;
            activeHand = aimSource;
            ropeLength = Vector3.Distance(transform.position, anchor);

            if (ropeLength < minRopeLength)
                return false;

            if (state == State.Grounded)
            {
                // Carry the walking speed into the swing so attaching does not feel like a stop.
                velocity = motor != null
                    ? motor.DesiredWorldMoveDirection * 4f
                    : Vector3.zero;
            }

            TakeControl();
            state = State.Swinging;
            return true;
        }

        private void TickSwing()
        {
            float dt = Time.deltaTime;

            velocity += Vector3.up * gravity * dt;
            velocity += AirSteering() * (airControl * dt);

            // Reeling in is what turns a dead hang into a rising arc.
            ropeLength = Mathf.Max(minRopeLength, ropeLength - reelInSpeed * dt);

            Vector3 predicted = transform.position + velocity * dt;
            Vector3 toAnchor = anchor - predicted;
            float distance = toAnchor.magnitude;

            Vector3 correction = Vector3.zero;
            if (distance > ropeLength && distance > .001f)
            {
                Vector3 ropeDirection = toAnchor / distance;

                // Rope can pull, never push: cancel only the outward radial velocity.
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
            activeHand = null;
        }

        private Vector3 AirSteering()
        {
            if (motor == null)
                return Vector3.zero;

            Vector3 steering = motor.DesiredWorldMoveDirection;
            steering.y = 0f;
            return steering;
        }

        /// <summary>Suspends the normal motor so only one system drives the controller.</summary>
        private void TakeControl()
        {
            if (motor != null)
                motor.enabled = false;
        }

        private void ReleaseControl()
        {
            state = State.Grounded;
            activeHand = null;
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

            if (webMaterial != null)
            {
                web.sharedMaterial = webMaterial;
            }
            else
            {
                // Unlit so the web stays visible against both the sky and dark geometry.
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material generated = new Material(shader) { color = Color.white };
                web.material = generated;
            }
        }

        private void UpdateWebVisual()
        {
            bool visible = state == State.Swinging && activeHand != null;
            web.enabled = visible;

            if (!visible)
                return;

            web.SetPosition(0, activeHand.position);
            web.SetPosition(1, anchor);
        }

        private void OnValidate()
        {
            gravity = Mathf.Min(-.01f, gravity);
            minRopeLength = Mathf.Max(1f, minRopeLength);
            maxWebRange = Mathf.Max(minRopeLength, maxWebRange);
        }
    }
}
