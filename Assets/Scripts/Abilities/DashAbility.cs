using UnityEngine;

namespace HeroVR.Abilities
{
    [DefaultExecutionOrder(25)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DashAbility : HeroAbility, IDirectionalAbility
    {
        [SerializeField] private Transform directionSource;
        [SerializeField, Min(0f)] private float distance = 5f;
        [SerializeField, Min(.05f)] private float duration = .22f;

        private CharacterController characterController;
        private Vector3 requestedDirection;
        private Vector3 dashDirection;
        private float elapsedDashTime;
        private float travelledDistance;

        public bool IsDashing { get; private set; }
        public float Distance => distance;
        public float Duration => duration;
        public float TravelledDistance => travelledDistance;

        protected override void Awake()
        {
            base.Awake();
            characterController = GetComponent<CharacterController>();
        }

        public void SetDirectionSource(Transform source)
        {
            directionSource = source;
        }

        public void SetDirection(Vector3 worldDirection)
        {
            requestedDirection = worldDirection;
        }

        public void SetDistance(float dashDistance)
        {
            distance = Mathf.Max(0f, dashDistance);
        }

        public void SetDuration(float dashDuration)
        {
            duration = Mathf.Max(.05f, dashDuration);
        }

        protected override bool CanActivate()
        {
            return characterController != null &&
                characterController.enabled &&
                !IsDashing &&
                distance > 0f;
        }

        protected override bool Activate()
        {
            Vector3 direction = requestedDirection;
            direction.y = 0f;

            if (direction.sqrMagnitude <= .0001f)
            {
                Transform source = directionSource != null ? directionSource : transform;
                direction = source.forward;
                direction.y = 0f;
            }

            dashDirection = direction.normalized;
            requestedDirection = Vector3.zero;
            elapsedDashTime = 0f;
            travelledDistance = 0f;
            IsDashing = true;
            return true;
        }

        private void Update()
        {
            if (!IsDashing || characterController == null || !characterController.enabled)
                return;

            float stepTime = Mathf.Min(Time.deltaTime, duration - elapsedDashTime);
            float stepDistance = distance / duration * Mathf.Max(0f, stepTime);
            Vector3 previousPosition = transform.position;
            characterController.Move(dashDirection * stepDistance);

            travelledDistance += Mathf.Max(
                0f,
                Vector3.Dot(transform.position - previousPosition, dashDirection));
            elapsedDashTime += stepTime;

            if (elapsedDashTime >= duration ||
                travelledDistance >= distance - .001f)
            {
                StopDash();
            }
        }

        private void OnDisable()
        {
            StopDash();
        }

        private void StopDash()
        {
            IsDashing = false;
            requestedDirection = Vector3.zero;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            distance = Mathf.Max(0f, distance);
            duration = Mathf.Max(.05f, duration);
        }
    }
}
