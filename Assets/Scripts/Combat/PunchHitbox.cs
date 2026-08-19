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
        private Damageable owner;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            owner = GetComponentInParent<Damageable>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            float speed = rb.linearVelocity.magnitude;
            if (speed < minimumHitSpeed) return;

            var target = collision.collider.GetComponentInParent<Damageable>();
            if (target == null || target == owner) return;

            float damage = Mathf.Min(speed * damagePerMeterPerSecond, maxDamage);
            Vector3 direction = rb.linearVelocity.normalized;
            float impulse = Mathf.Min(speed * knockbackMultiplier, maxKnockbackImpulse);
            Vector3 hitPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

            target.TakeDamage(new DamageInfo(
                damage,
                owner != null ? owner.gameObject : gameObject,
                hitPoint,
                direction,
                impulse));

            if (collision.rigidbody != null && !collision.rigidbody.isKinematic)
            {
                collision.rigidbody.AddForce(direction * impulse, ForceMode.Impulse);
            }
        }
    }
}
