using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroVR.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class ImpactDamageDealer : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float minimumDamagingSpeed = 3f;
        [SerializeField, Min(0f)] private float minimumMomentum = 6f;
        [SerializeField, Min(0f)] private float damagePerMomentum = .7f;
        [SerializeField, Min(0f)] private float maximumDamage = 80f;
        [SerializeField, Min(0f)] private float impulseToKnockback = .65f;
        [SerializeField, Min(0f)] private float maximumKnockbackImpulse = 32f;
        [SerializeField, Min(0f)] private float impactStrengthScale = 1f;
        [SerializeField, Min(0f)] private float perTargetCooldown = .18f;

        private readonly Dictionary<Transform, float> nextImpactTimes =
            new Dictionary<Transform, float>();
        private Rigidbody body;
        private Transform instigatorRoot;

        public GameObject Instigator { get; private set; }
        public ImpactDamageResult LastImpact { get; private set; }
        public event Action<DamageInfo> DamagingImpact;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void Configure(
            float minimumSpeed,
            float minimumRequiredMomentum,
            float damageScaling,
            float damageCap,
            float knockbackScaling,
            float knockbackCap,
            float strengthScale,
            float targetCooldown)
        {
            minimumDamagingSpeed = Mathf.Max(0f, minimumSpeed);
            minimumMomentum = Mathf.Max(0f, minimumRequiredMomentum);
            damagePerMomentum = Mathf.Max(0f, damageScaling);
            maximumDamage = Mathf.Max(0f, damageCap);
            impulseToKnockback = Mathf.Max(0f, knockbackScaling);
            maximumKnockbackImpulse = Mathf.Max(0f, knockbackCap);
            impactStrengthScale = Mathf.Max(0f, strengthScale);
            perTargetCooldown = Mathf.Max(0f, targetCooldown);
        }

        public void SetInstigator(GameObject damageInstigator)
        {
            Instigator = damageInstigator;
            instigatorRoot = damageInstigator != null
                ? damageInstigator.transform.root
                : null;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider == null)
                return;

            Vector3 point = collision.contactCount > 0
                ? collision.GetContact(0).point
                : collision.collider.ClosestPoint(transform.position);
            TryApplyImpact(
                collision.collider,
                collision.relativeVelocity,
                collision.impulse.magnitude,
                point);
        }

        public bool TryApplyImpact(
            Collider targetCollider,
            Vector3 relativeVelocity,
            float collisionImpulse,
            Vector3 hitPoint)
        {
            if (targetCollider == null ||
                targetCollider.transform.root == instigatorRoot)
            {
                return false;
            }

            Transform targetRoot = targetCollider.transform.root;
            if (nextImpactTimes.TryGetValue(targetRoot, out float nextTime) &&
                Time.time < nextTime)
            {
                return false;
            }

            LastImpact = ImpactDamageModel.Calculate(
                body.mass,
                relativeVelocity.magnitude,
                collisionImpulse,
                minimumDamagingSpeed,
                minimumMomentum,
                damagePerMomentum,
                maximumDamage,
                impulseToKnockback,
                maximumKnockbackImpulse,
                impactStrengthScale);
            if (!LastImpact.IsDamaging)
                return false;

            Vector3 direction = relativeVelocity.sqrMagnitude > .0001f
                ? relativeVelocity.normalized
                : transform.forward;
            DamageInfo damageInfo = new DamageInfo(
                LastImpact.Damage,
                Instigator != null ? Instigator : gameObject,
                hitPoint,
                direction,
                LastImpact.KnockbackImpulse,
                LastImpact.ImpactStrength,
                ImpactSeverityUtility.PhysicalDamageType(
                    LastImpact.ImpactStrength));

            if (CombatHitResolver.Apply(targetCollider, damageInfo) > 0)
            {
                nextImpactTimes[targetRoot] = Time.time + perTargetCooldown;
                DamagingImpact?.Invoke(damageInfo);
                return true;
            }

            return false;
        }

        private void OnDisable()
        {
            nextImpactTimes.Clear();
        }

        private void OnValidate()
        {
            minimumDamagingSpeed = Mathf.Max(0f, minimumDamagingSpeed);
            minimumMomentum = Mathf.Max(0f, minimumMomentum);
            damagePerMomentum = Mathf.Max(0f, damagePerMomentum);
            maximumDamage = Mathf.Max(0f, maximumDamage);
            impulseToKnockback = Mathf.Max(0f, impulseToKnockback);
            maximumKnockbackImpulse = Mathf.Max(0f, maximumKnockbackImpulse);
            impactStrengthScale = Mathf.Max(0f, impactStrengthScale);
            perTargetCooldown = Mathf.Max(0f, perTargetCooldown);
        }
    }
}
