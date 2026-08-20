using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.XR
{
    [DefaultExecutionOrder(0)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class XRCharacterMotor : MonoBehaviour
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
        private bool jumpRequested;

        public Transform Head => head;
        public float MoveSpeed => moveSpeed;
        public float JumpHeight => jumpHeight;
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
                jumpRequested = false;
                return;
            }

            if (characterController.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;

            if (jumpRequested && characterController.isGrounded)
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

            jumpRequested = false;
            verticalSpeed += gravity * Time.deltaTime;

            Vector3 velocity =
                DesiredWorldMoveDirection * moveSpeed + transform.up * verticalSpeed;
            characterController.Move(velocity * Time.deltaTime);
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
