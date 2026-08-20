using HeroVR.Combat;
using HeroVR.Movement;
using UnityEngine;

namespace HeroVR.XR
{
    [DefaultExecutionOrder(0)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class XRCharacterMotor : MonoBehaviour, IFlightMovementReceiver
    {
        [SerializeField] private Transform head;
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField, Min(0f)] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -18f;
        [SerializeField, Range(15f, 90f)] private float snapTurnAngle = 30f;
        [SerializeField, Min(.5f)] private float minimumBodyHeight = .8f;
        [SerializeField, Min(.5f)] private float maximumBodyHeight = 2.2f;

        private CharacterController characterController;
        private Damageable health;
        private Vector2 moveInput;
        private float verticalSpeed;
        private Vector3 flightVelocity;
        private Vector3 velocity;
        private float flightGravityScale = 1f;
        private float flightLiftAcceleration;
        private float flightDownwardDamping;
        private float flightMaximumClimbSpeed;
        private float flightAirSteeringMultiplier = 1f;
        private float flightDrag;
        private float flightMaximumHorizontalSpeed;
        private bool jumpRequested;

        public Transform Head => head;
        public float MoveSpeed => moveSpeed;
        public float JumpHeight => jumpHeight;
        public bool IsGrounded => characterController != null &&
            characterController.isGrounded;
        public Vector3 Velocity => velocity;
        public Vector3 FlightVelocity => flightVelocity;
        public Vector3 DesiredWorldMoveDirection
        {
            get
            {
                Transform directionSource = head != null ? head : transform;
                Vector3 forward = Vector3.ProjectOnPlane(
                    directionSource.forward,
                    transform.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(
                    directionSource.right,
                    transform.up).normalized;
                Vector3 direction = right * moveInput.x + forward * moveInput.y;
                return direction.sqrMagnitude > 1f ? direction.normalized : direction;
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            health = GetComponent<Damageable>();
        }

        public void Configure(Transform headTransform)
        {
            head = headTransform;
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void RequestJump()
        {
            jumpRequested = true;
        }

        public void ConfigureMovement(float speed, float height)
        {
            moveSpeed = Mathf.Max(0f, speed);
            jumpHeight = Mathf.Max(0f, height);
        }

        public void AddFlightImpulse(Vector3 impulse, float maximumSpeed)
        {
            flightVelocity += impulse;
            if (maximumSpeed > 0f)
                flightVelocity = Vector3.ClampMagnitude(flightVelocity, maximumSpeed);
        }

        public void SetFlightModifiers(
            float gravityScale,
            float liftAcceleration,
            float downwardDamping,
            float maximumClimbSpeed,
            float airSteeringMultiplier,
            float drag,
            float maximumHorizontalSpeed)
        {
            flightGravityScale = Mathf.Clamp01(gravityScale);
            flightLiftAcceleration = Mathf.Max(0f, liftAcceleration);
            flightDownwardDamping = Mathf.Max(0f, downwardDamping);
            flightMaximumClimbSpeed = Mathf.Max(0f, maximumClimbSpeed);
            flightAirSteeringMultiplier = Mathf.Clamp01(airSteeringMultiplier);
            flightDrag = Mathf.Max(0f, drag);
            flightMaximumHorizontalSpeed = Mathf.Max(0f, maximumHorizontalSpeed);
        }

        public void ResetFlightMotion()
        {
            flightVelocity = Vector3.zero;
            flightGravityScale = 1f;
            flightLiftAcceleration = 0f;
            flightDownwardDamping = 0f;
            flightMaximumClimbSpeed = 0f;
            flightAirSteeringMultiplier = 1f;
            flightDrag = 0f;
            flightMaximumHorizontalSpeed = 0f;
        }

        public void RequestSnapTurn(float direction)
        {
            if (Mathf.Abs(direction) <= .01f)
                return;

            float angle = Mathf.Sign(direction) * snapTurnAngle;
            Vector3 pivot = head != null ? head.position : transform.position;
            transform.RotateAround(pivot, transform.up, angle);
        }

        private void Update()
        {
            SyncBodyColliderToHead();

            if (health != null && health.IsDead)
            {
                moveInput = Vector2.zero;
                verticalSpeed = 0f;
                velocity = Vector3.zero;
                jumpRequested = false;
                ResetFlightMotion();
                return;
            }

            bool grounded = characterController.isGrounded;
            if (grounded && verticalSpeed < 0f)
                verticalSpeed = -2f;

            if (jumpRequested && grounded)
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

            jumpRequested = false;
            float gravityScale = grounded ? 1f : flightGravityScale;
            verticalSpeed += gravity * gravityScale * Time.deltaTime;
            if (!grounded && flightLiftAcceleration > 0f)
            {
                if (verticalSpeed < 0f && flightDownwardDamping > 0f)
                {
                    verticalSpeed = Mathf.MoveTowards(
                        verticalSpeed,
                        0f,
                        flightDownwardDamping * Time.deltaTime);
                }
                verticalSpeed += flightLiftAcceleration * Time.deltaTime;
                if (flightMaximumClimbSpeed > 0f)
                    verticalSpeed = Mathf.Min(verticalSpeed, flightMaximumClimbSpeed);
            }

            float steeringMultiplier = grounded ? 1f : flightAirSteeringMultiplier;
            Vector3 horizontal =
                DesiredWorldMoveDirection * moveSpeed * steeringMultiplier;
            Vector3 combinedHorizontal = horizontal +
                Vector3.ProjectOnPlane(flightVelocity, transform.up);
            if (flightMaximumHorizontalSpeed > 0f)
            {
                combinedHorizontal = Vector3.ClampMagnitude(
                    combinedHorizontal,
                    flightMaximumHorizontalSpeed);
            }

            Vector3 intendedVelocity = combinedHorizontal +
                transform.up * verticalSpeed +
                transform.up * Vector3.Dot(flightVelocity, transform.up);
            Vector3 previousPosition = transform.position;
            CollisionFlags collisions = characterController.Move(
                intendedVelocity * Time.deltaTime);
            velocity = (transform.position - previousPosition) /
                Mathf.Max(Time.deltaTime, .0001f);
            ResolveFlightCollisions(collisions);
            if (flightDrag > 0f)
            {
                flightVelocity = Vector3.MoveTowards(
                    flightVelocity,
                    Vector3.zero,
                    flightDrag * Time.deltaTime);
            }
        }

        private void ResolveFlightCollisions(CollisionFlags collisions)
        {
            if ((collisions & CollisionFlags.Sides) != 0)
            {
                float vertical = Vector3.Dot(flightVelocity, transform.up);
                flightVelocity = transform.up * vertical;
            }

            float flightVerticalSpeed = Vector3.Dot(flightVelocity, transform.up);
            if ((collisions & CollisionFlags.Above) != 0 && flightVerticalSpeed > 0f)
                flightVelocity -= transform.up * flightVerticalSpeed;
            else if ((collisions & CollisionFlags.Below) != 0 && flightVerticalSpeed < 0f)
                flightVelocity -= transform.up * flightVerticalSpeed;
        }

        private void SyncBodyColliderToHead()
        {
            if (head == null)
                return;

            Vector3 localHead = transform.InverseTransformPoint(head.position);
            float height = Mathf.Clamp(
                localHead.y,
                minimumBodyHeight,
                maximumBodyHeight);

            characterController.height = height;
            characterController.center = new Vector3(
                localHead.x,
                height * .5f,
                localHead.z);
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            gravity = Mathf.Min(-.01f, gravity);
            minimumBodyHeight = Mathf.Max(.5f, minimumBodyHeight);
            maximumBodyHeight = Mathf.Max(minimumBodyHeight, maximumBodyHeight);
        }
    }
}
