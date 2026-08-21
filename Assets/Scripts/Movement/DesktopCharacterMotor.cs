using UnityEngine;
using HeroVR.Combat;

namespace HeroVR.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DesktopCharacterMotor : MonoBehaviour, IFlightMovementReceiver
    {
        [SerializeField, Min(0f)] private float moveSpeed = 7f;
        [SerializeField, Min(0f)] private float jumpHeight = 2.6f;
        [SerializeField] private float gravity = -22f;
        [SerializeField, Min(0f)] private float lookSensitivity = .12f;
        [SerializeField] private Transform viewTransform;

        private CharacterController characterController;
        private Damageable health;
        private Vector2 moveInput;
        private Vector2 pendingLookDelta;
        private Vector3 verticalVelocity;
        private Vector3 flightVelocity;
        private Vector3 velocity;
        private float flightGravityScale = 1f;
        private float flightLiftAcceleration;
        private float flightDownwardDamping;
        private float flightMaximumClimbSpeed;
        private float flightAirSteeringMultiplier = 1f;
        private float flightDrag;
        private float flightMaximumHorizontalSpeed;
        private float pitch;
        private bool jumpRequested;

        public Transform ViewTransform => viewTransform;
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
                Vector3 direction =
                    transform.right * moveInput.x + transform.forward * moveInput.y;
                return direction.sqrMagnitude > 1f ? direction.normalized : direction;
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            health = GetComponent<Damageable>();

            if (viewTransform != null)
                pitch = NormalizePitch(viewTransform.localEulerAngles.x);
        }

        public void SetViewTransform(Transform view)
        {
            viewTransform = view;
            if (viewTransform != null)
                pitch = NormalizePitch(viewTransform.localEulerAngles.x);
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void AddLookDelta(Vector2 delta)
        {
            pendingLookDelta += delta;
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

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                moveInput = Vector2.zero;
                pendingLookDelta = Vector2.zero;
                verticalVelocity = Vector3.zero;
                velocity = Vector3.zero;
                jumpRequested = false;
                ResetFlightMotion();
                return;
            }

            ApplyLook();
            ApplyMovement();
        }

        private void ApplyLook()
        {
            Vector2 look = pendingLookDelta * lookSensitivity;
            pendingLookDelta = Vector2.zero;

            transform.Rotate(Vector3.up * look.x);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);

            if (viewTransform != null)
                viewTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ApplyMovement()
        {
            bool grounded = characterController.isGrounded;
            if (grounded && verticalVelocity.y < 0f)
                verticalVelocity.y = -2f;

            if (jumpRequested && grounded)
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            jumpRequested = false;
            float gravityScale = grounded ? 1f : flightGravityScale;
            verticalVelocity.y += gravity * gravityScale * Time.deltaTime;
            if (!grounded && flightLiftAcceleration > 0f)
            {
                if (verticalVelocity.y < 0f && flightDownwardDamping > 0f)
                {
                    verticalVelocity.y = Mathf.MoveTowards(
                        verticalVelocity.y,
                        0f,
                        flightDownwardDamping * Time.deltaTime);
                }
                verticalVelocity.y += flightLiftAcceleration * Time.deltaTime;
                if (flightMaximumClimbSpeed > 0f)
                {
                    verticalVelocity.y = Mathf.Min(
                        verticalVelocity.y,
                        flightMaximumClimbSpeed);
                }
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
                transform.up * verticalVelocity.y +
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

            float verticalSpeed = Vector3.Dot(flightVelocity, transform.up);
            if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
                flightVelocity -= transform.up * verticalSpeed;
            else if ((collisions & CollisionFlags.Below) != 0 && verticalSpeed < 0f)
                flightVelocity -= transform.up * verticalSpeed;
        }

        private static float NormalizePitch(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            lookSensitivity = Mathf.Max(0f, lookSensitivity);
            gravity = Mathf.Min(-.01f, gravity);
        }
    }
}
