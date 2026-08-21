using System.Collections.Generic;
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
        [SerializeField, Min(0f)] private float contactCooldown = .2f;

        private Rigidbody rb;
        private Damageable owner;
        private IHitVelocityProvider velocityProvider;
        private readonly Dictionary<Damageable, float> nextHitTimes =
            new Dictionary<Damageable, float>();

        public Damageable Owner => owner;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            owner = GetComponentInParent<Damageable>();

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IHitVelocityProvider provider)
                {
                    velocityProvider = provider;
                    break;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Vector3 velocity = velocityProvider != null
                ? velocityProvider.Velocity
                : rb.linearVelocity;
            float speed = velocity.magnitude;
            if (speed < minimumHitSpeed) return;

            var target = collision.collider.GetComponentInParent<Damageable>();
            if (target == owner || target != null && target.IsDead)
                return;

            if (target != null &&
                nextHitTimes.TryGetValue(target, out float nextHitTime) &&
                Time.time < nextHitTime)
            {
                return;
            }

            float damage = Mathf.Min(speed * damagePerMeterPerSecond, maxDamage);
            Vector3 direction = velocity.normalized;
            float impulse = Mathf.Min(speed * knockbackMultiplier, maxKnockbackImpulse);
            Vector3 hitPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

            DamageInfo damageInfo = new DamageInfo(
                damage,
                owner != null ? owner.gameObject : gameObject,
                hitPoint,
                direction,
                impulse,
                impulse,
                ImpactSeverityUtility.PhysicalDamageType(impulse));

            if (CombatHitResolver.Apply(collision.collider, damageInfo) <= 0)
                return;

            if (target != null)
                nextHitTimes[target] = Time.time + contactCooldown;

            if (collision.rigidbody != null && !collision.rigidbody.isKinematic)
            {
                collision.rigidbody.AddForce(direction * impulse, ForceMode.Impulse);
            }
        }

        public void Configure(
            float minimumSpeed,
            float damagePerSpeed,
            float maximumDamage,
            float impulseMultiplier,
            float maximumImpulse)
        {
            minimumHitSpeed = Mathf.Max(0f, minimumSpeed);
            damagePerMeterPerSecond = Mathf.Max(0f, damagePerSpeed);
            maxDamage = Mathf.Max(0f, maximumDamage);
            knockbackMultiplier = Mathf.Max(0f, impulseMultiplier);
            maxKnockbackImpulse = Mathf.Max(0f, maximumImpulse);
        }

        public void Configure(
            float minimumSpeed,
            float damagePerSpeed,
            float maximumDamage,
            float impulseMultiplier,
            float maximumImpulse,
            float perTargetContactCooldown)
        {
            Configure(
                minimumSpeed,
                damagePerSpeed,
                maximumDamage,
                impulseMultiplier,
                maximumImpulse);
            contactCooldown = Mathf.Max(0f, perTargetContactCooldown);
        }

        public void SetOwner(Damageable damageOwner)
        {
            owner = damageOwner;
        }

        private void OnDisable()
        {
            nextHitTimes.Clear();
        }

        private void OnValidate()
        {
            minimumHitSpeed = Mathf.Max(0f, minimumHitSpeed);
            damagePerMeterPerSecond = Mathf.Max(0f, damagePerMeterPerSecond);
            maxDamage = Mathf.Max(0f, maxDamage);
            knockbackMultiplier = Mathf.Max(0f, knockbackMultiplier);
            maxKnockbackImpulse = Mathf.Max(0f, maxKnockbackImpulse);
            contactCooldown = Mathf.Max(0f, contactCooldown);
        }
    }
}
