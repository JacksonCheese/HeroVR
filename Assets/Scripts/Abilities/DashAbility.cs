using UnityEngine;

namespace HeroVR.Abilities
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DashAbility : HeroAbility
    {
        [SerializeField] private Transform directionSource;
        [SerializeField, Min(0f)] private float distance = 5f;

        private CharacterController characterController;

        protected override void Awake()
        {
            base.Awake();
            characterController = GetComponent<CharacterController>();
        }

        public void SetDirectionSource(Transform source)
        {
            directionSource = source;
        }

        protected override bool CanActivate()
        {
            return characterController != null && characterController.enabled;
        }

        protected override bool Activate()
        {
            Transform source = directionSource != null ? directionSource : transform;
            Vector3 direction = source.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude <= .0001f)
                direction = transform.forward;

            characterController.Move(direction.normalized * distance);
            return true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            distance = Mathf.Max(0f, distance);
        }
    }
}
