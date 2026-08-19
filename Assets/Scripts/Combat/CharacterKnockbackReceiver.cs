using UnityEngine;

namespace HeroVR.Combat
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(Damageable))]
    public sealed class CharacterKnockbackReceiver : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float impulseToSpeed = 1f;
        [SerializeField, Min(0f)] private float deceleration = 14f;

        private CharacterController characterController;
        private Damageable damageable;
        private Vector3 knockbackVelocity;

        public Vector3 KnockbackVelocity => knockbackVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            damageable = GetComponent<Damageable>();
        }

        private void OnEnable()
        {
            damageable.Damaged += OnDamaged;
            damageable.Died += ClearKnockback;
        }

        private void OnDisable()
        {
            damageable.Damaged -= OnDamaged;
            damageable.Died -= ClearKnockback;
            ClearKnockback();
        }

        public void ApplyKnockback(Vector3 direction, float impulse)
        {
            if (impulse <= 0f || direction.sqrMagnitude <= .0001f)
                return;

            knockbackVelocity += direction.normalized * impulse * impulseToSpeed;
        }

        private void Update()
        {
            if (!characterController.enabled || knockbackVelocity.sqrMagnitude <= .0001f)
                return;

            characterController.Move(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector3.MoveTowards(
                knockbackVelocity,
                Vector3.zero,
                deceleration * Time.deltaTime);
        }

        private void OnDamaged(DamageInfo damageInfo)
        {
            ApplyKnockback(damageInfo.Direction, damageInfo.KnockbackImpulse);
        }

        private void ClearKnockback()
        {
            knockbackVelocity = Vector3.zero;
        }

        private void OnValidate()
        {
            impulseToSpeed = Mathf.Max(0f, impulseToSpeed);
            deceleration = Mathf.Max(0f, deceleration);
        }
    }
}
