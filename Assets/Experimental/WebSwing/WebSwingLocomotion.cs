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
    /// whole airborne phase and hands it back on landing, so the two never fight. The proper fix
    /// is an external-velocity API on XRCharacterMotor; that is a gameplay-side change.
    ///
    /// Aiming uses the tracked controller transforms, not the physics hands: the physics hands lag
    /// the real pose and rotate under physics. Webs are still drawn from the physics hands, since
    /// those are the objects the player sees.
    ///
    /// Head-through-hand aiming was tried and removed. The hand sits below the head, so that ray
    /// points downward and webs stuck to the floor - fine for targets at eye level, wrong for
    /// slinging at things overhead.
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

        [Header("Aim sources (tracked controllers)")]
        [SerializeField] private Transform leftAim;
        [SerializeField] private Transform rightAim;
        [SerializeField] private Transform head;

        [Header("Web origins (physics hands - what the player sees)")]
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;

        [Header("Aiming")]
        [SerializeField, Min(1f)] private float maxWebRange = 35f;
        [Tooltip("0 is a pure raycast. Small values forgive shaky hands. Above about 0.4 it " +
                 "starts snapping to things you did not aim at.")]
        [SerializeField, Range(0f, 1f)] private float aimAssistRadius = .25f;
        [SerializeField] private LayerMask anchorLayers = ~0;
        [Tooltip("Positive tilts the aim DOWN, negative tilts it UP. The controller's forward axis " +
                 "sits higher than where the index finger points, so a positive value is normally " +
                 "needed to line the web up with the trigger hand.")]
        [SerializeField, Range(-45f, 45f)] private float aimPitchOffset = 15f;

        [Tooltip("Where the web leaves the hand, local to the controller. The tracked origin sits " +
                 "behind and above the trigger, so pushing forward (+Z) and down (-Y) puts the " +
                 "web at the index finger instead of floating above the wrist.")]
        [SerializeField] private Vector3 webOriginOffset = new Vector3(0f, -.022f, .045f);

        [Header("Swing feel")]
        [SerializeField] private float gravity = -13f;
        [SerializeField, Min(0f)] private float reelInSpeed = 5.2f;
        [SerializeField, Min(1f)] private float minRopeLength = 2.5f;
        [SerializeField, Min(0f)] private float airControl = 5f;
        [SerializeField, Min(0f)] private float releaseBoost = 3.5f;
        [SerializeField, Min(0f)] private float attachSpeedCarry = 5f;
        [SerializeField, Min(0f)] private float maxSpeed = 30f;

        [Tooltip("Speed injected along the arc the moment a web catches. Without this, attaching " +
                 "from a standstill leaves you hanging still instead of swinging.")]
        [SerializeField, Min(0f)] private float attachImpulse = 9f;

        [Tooltip("Continuous push along the direction of travel while swinging. This is the " +
                 "pumping that keeps arcs alive instead of decaying into a dead hang.")]
        [SerializeField, Min(0f)] private float swingThrust = 9f;

        [Tooltip("Keep control after touching down while still moving this fast, so a swing " +
                 "runs out along the ground instead of stopping dead on contact.")]
        [SerializeField, Min(0f)] private float groundedExitSpeed = 4.5f;

        [Tooltip("How hard a landing slide scrubs speed. Too low and you skate forever, too high " +
                 "and landings still feel like hitting a wall.")]
        [SerializeField, Min(0f)] private float landingFriction = 14f;
        [Tooltip("Ignore anchors below this height difference so webs do not stick to the floor " +
                 "at your feet, which reads as the swing failing.")]
        [SerializeField] private float minAnchorHeightAboveFeet = 1.5f;

        [Header("Visuals")]
        [SerializeField, Min(.005f)] private float webThickness = .05f;
        [SerializeField, Min(0f)] private float missVisualDuration = .35f;
        [Tooltip("Draw a thin ray from each hand showing exactly where a web would fire. " +
                 "Invaluable for checking aim; turn off once it feels right.")]
        [SerializeField] private bool showAimRay = true;
        [SerializeField, Min(.001f)] private float aimRayThickness = .012f;
        [SerializeField, Min(1f)] private float aimRayLength = 12f;

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
        private LineRenderer leftRay;
        private LineRenderer rightRay;

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

            web = CreateLine("WebLine", webThickness, Color.white);
            leftRay = CreateLine("AimRayLeft", aimRayThickness, new Color(.4f, .8f, 1f));
            rightRay = CreateLine("AimRayRight", aimRayThickness, new Color(.4f, .8f, 1f));
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
                UpdateVisuals();
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

            switch (state)
            {
                case State.Swinging:
                    if (!leftHeld && !rightHeld)
                        Release();
                    else
                        TickSwing();
                    break;

                case State.Airborne:
                    TickAirborne();
                    break;
            }

            UpdateVisuals();
        }

        private void FireWeb(Transform aim, Transform origin)
        {
            Transform aimSource = aim != null ? aim : head;
            if (aimSource == null)
                return;

            Transform visualOrigin = origin != null ? origin : aimSource;
            Vector3 direction = AimDirection(aimSource);

            // Fire from the index finger rather than the tracked origin, so the web leaves the
            // hand where the player expects it to.
            Vector3 rayStart = WebOrigin(aimSource);

            RaycastHit hit;
            bool hitSomething = aimAssistRadius <= .001f
                ? Physics.Raycast(rayStart, direction, out hit, maxWebRange, anchorLayers,
                    QueryTriggerInteraction.Ignore)
                : Physics.SphereCast(rayStart, aimAssistRadius, direction, out hit, maxWebRange,
                    anchorLayers, QueryTriggerInteraction.Ignore);

            if (!hitSomething || hit.transform.root == transform.root)
            {
                ShowMiss(aimSource, rayStart + direction * maxWebRange);
                return;
            }

            // Anchoring to the ground under your own feet reads as the web failing, so require
            // the anchor to be meaningfully above the player.
            if (hit.point.y < transform.position.y + minAnchorHeightAboveFeet)
            {
                ShowMiss(aimSource, hit.point);
                return;
            }

            anchor = hit.point;

            // Track the aim transform, not the hand mesh, so the drawn web starts at the same
            // muzzle point the raycast fired from.
            activeOrigin = aimSource;
            ropeLength = Mathf.Max(minRopeLength, Vector3.Distance(transform.position, anchor));

            if (state == State.Grounded)
            {
                Vector3 carry = motor != null ? motor.DesiredWorldMoveDirection : Vector3.zero;
                velocity = carry * attachSpeedCarry;
            }

            // Kick off along the arc. Without this, catching a web while standing still left the
            // player hanging motionless: gravity alone takes far too long to build a swing, which
            // read as the web doing nothing.
            Vector3 ropeUp = (anchor - transform.position).normalized;
            Vector3 launchHint = head != null ? head.forward : transform.forward;
            Vector3 tangent = Vector3.ProjectOnPlane(launchHint, ropeUp);

            if (tangent.sqrMagnitude > .0001f)
                velocity += tangent.normalized * attachImpulse;

            missUntil = 0f;
            TakeControl();
            state = State.Swinging;
        }

        private Vector3 AimDirection(Transform aimSource)
        {
            if (Mathf.Abs(aimPitchOffset) < .01f)
                return aimSource.forward;

            // Positive rotates +Z toward -Y, i.e. downward.
            return Quaternion.AngleAxis(aimPitchOffset, aimSource.right) * aimSource.forward;
        }

        /// <summary>Muzzle point in world space: the index finger, not the tracked origin.</summary>
        private Vector3 WebOrigin(Transform aimSource)
        {
            return aimSource.TransformPoint(webOriginOffset);
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

            // Pump along the arc. A real pendulum bleeds energy every frame the rope constraint
            // cancels radial velocity, so without a push along the direction of travel the swing
            // decays into a dead hang after a couple of arcs.
            Vector3 ropeUp = (anchor - transform.position).normalized;
            Vector3 alongArc = Vector3.ProjectOnPlane(velocity, ropeUp);

            if (alongArc.sqrMagnitude > .01f)
                velocity += alongArc.normalized * (swingThrust * dt);

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

            if (ShouldHandBackControl())
                ReleaseControl();
        }

        private void TickAirborne()
        {
            float dt = Time.deltaTime;

            if (controller.isGrounded && velocity.y < 0f)
            {
                // Sliding out a landing rather than falling. Stop gravity accumulating into a
                // huge downward figure, and scrub horizontal speed so the slide actually ends
                // and control returns instead of skating forever.
                velocity.y = -2f;

                Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
                horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, landingFriction * dt);
                velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
            }
            else
            {
                velocity += Vector3.up * gravity * dt;
            }

            velocity += AirSteering() * (airControl * dt);
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

            controller.Move(velocity * dt);

            if (ShouldHandBackControl())
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

        /// <summary>
        /// Landing alone is not enough to end a swing. Handing control straight back on the first
        /// ground contact stopped the player dead the instant they clipped a rooftop, which killed
        /// every long swing. Control is only returned once they are actually slow.
        /// </summary>
        private bool ShouldHandBackControl()
        {
            if (!controller.isGrounded || velocity.y > 0f)
                return false;

            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            return horizontal.magnitude <= groundedExitSpeed;
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

        private LineRenderer CreateLine(string lineName, float thickness, Color color)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = thickness;
            line.endWidth = thickness;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;

            // Unlit so it reads against both bright sky and dark geometry. Falls back through
            // several shader names because availability varies by pipeline.
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = color;

            // Standard ignores plain colour on an emissive-less material at distance, so push
            // emission too when that is what we fell back to.
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color);
            }

            line.material = material;
            line.startColor = color;
            line.endColor = color;
            return line;
        }

        private void UpdateVisuals()
        {
            UpdateWebLine();
            UpdateAimRay(leftRay, leftAim, leftHand);
            UpdateAimRay(rightRay, rightAim, rightHand);
        }

        private void UpdateWebLine()
        {
            if (state == State.Swinging && activeOrigin != null)
            {
                web.enabled = true;
                web.SetPosition(0, WebOrigin(activeOrigin));
                web.SetPosition(1, anchor);
                return;
            }

            if (Time.time < missUntil && missOrigin != null)
            {
                web.enabled = true;
                web.SetPosition(0, WebOrigin(missOrigin));
                web.SetPosition(1, missEnd);
                return;
            }

            web.enabled = false;
        }

        private void UpdateAimRay(LineRenderer ray, Transform aim, Transform origin)
        {
            if (!showAimRay || aim == null || state == State.Swinging)
            {
                ray.enabled = false;
                return;
            }

            // Ray must start at the muzzle and follow the same direction the raycast uses, or it
            // shows the player something other than where the web will actually go.
            Vector3 muzzle = WebOrigin(aim);
            ray.enabled = true;
            ray.SetPosition(0, muzzle);
            ray.SetPosition(1, muzzle + AimDirection(aim) * aimRayLength);
        }

        private void OnValidate()
        {
            gravity = Mathf.Min(-.01f, gravity);
            minRopeLength = Mathf.Max(1f, minRopeLength);
            maxWebRange = Mathf.Max(minRopeLength, maxWebRange);
        }
    }
}
