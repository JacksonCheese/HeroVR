using UnityEngine;
using HeroVR.Combat;

namespace HeroVR.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DesktopCharacterMotor : MonoBehaviour
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
        private float pitch;
        private bool jumpRequested;

        public Transform ViewTransform => viewTransform;
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

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                moveInput = Vector2.zero;
                pendingLookDelta = Vector2.zero;
                verticalVelocity = Vector3.zero;
                jumpRequested = false;
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
            if (characterController.isGrounded && verticalVelocity.y < 0f)
                verticalVelocity.y = -2f;

            if (jumpRequested && characterController.isGrounded)
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            jumpRequested = false;
            verticalVelocity.y += gravity * Time.deltaTime;

            Vector3 horizontal = DesiredWorldMoveDirection * moveSpeed;

            characterController.Move((horizontal + verticalVelocity) * Time.deltaTime);
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
