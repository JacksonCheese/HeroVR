using UnityEngine;

namespace HeroVR.Abilities
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DashAbility : HeroAbility, IDirectionalAbility
    {
        [SerializeField] private Transform directionSource;
        [SerializeField, Min(0f)] private float distance = 5f;

        private CharacterController characterController;
        private Vector3 requestedDirection;

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

        protected override bool CanActivate()
        {
            return characterController != null && characterController.enabled;
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

            characterController.Move(direction.normalized * distance);
            requestedDirection = Vector3.zero;
            return true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            distance = Mathf.Max(0f, distance);
        }
    }
}
