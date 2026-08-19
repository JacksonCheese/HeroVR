using UnityEngine;

namespace HeroVR.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class PunchHitbox : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float minimumHitSpeed = 1.5f;
        [SerializeField] private float damagePerMeterPerSecond = 7f;
        [SerializeField] private float maxDamage = 35f;

        [Header("Knockback")]
        [SerializeField] private float knockbackMultiplier = 1.8f;
        [SerializeField] private float maxKnockbackImpulse = 12f;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            float speed = rb.linearVelocity.magnitude;
            if (speed < minimumHitSpeed) return;

            var target = collision.collider.GetComponentInParent<Damageable>();
            if (target == null) return;

            float damage = Mathf.Min(speed * damagePerMeterPerSecond, maxDamage);
            target.TakeDamage(damage);

            if (collision.rigidbody != null && !collision.rigidbody.isKinematic)
            {
                Vector3 direction = rb.linearVelocity.normalized;
                float impulse = Mathf.Min(speed * knockbackMultiplier, maxKnockbackImpulse);
                collision.rigidbody.AddForce(direction * impulse, ForceMode.Impulse);
            }
        }
    }
}
