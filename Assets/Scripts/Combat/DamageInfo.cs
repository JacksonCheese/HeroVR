using UnityEngine;

namespace HeroVR.Combat
{
    public readonly struct DamageInfo
    {
        public DamageInfo(
            float amount,
            GameObject instigator = null,
            Vector3 point = default,
            Vector3 direction = default,
            float knockbackImpulse = 0f,
            float impactStrength = -1f,
            DamageType damageType = DamageType.Physical)
        {
            Amount = amount;
            Instigator = instigator;
            Point = point;
            Direction = direction.sqrMagnitude > .0001f
                ? direction.normalized
                : Vector3.zero;
            KnockbackImpulse = Mathf.Max(0f, knockbackImpulse);
            ImpactStrength = impactStrength >= 0f
                ? impactStrength
                : Mathf.Max(KnockbackImpulse, Amount * .25f);
            DamageType = damageType;
        }

        public float Amount { get; }
        public GameObject Instigator { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public float KnockbackImpulse { get; }
        public float ImpactStrength { get; }
        public DamageType DamageType { get; }
        public ImpactSeverity Severity => ImpactSeverityUtility.Classify(ImpactStrength);

        public DamageInfo Scaled(float damageMultiplier, float impactMultiplier = 1f)
        {
            return new DamageInfo(
                Amount * Mathf.Max(0f, damageMultiplier),
                Instigator,
                Point,
                Direction,
                KnockbackImpulse * Mathf.Max(0f, impactMultiplier),
                ImpactStrength * Mathf.Max(0f, impactMultiplier),
                DamageType);
        }
    }
}
