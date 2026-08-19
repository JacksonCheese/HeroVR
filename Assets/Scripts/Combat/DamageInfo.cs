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
            float knockbackImpulse = 0f)
        {
            Amount = amount;
            Instigator = instigator;
            Point = point;
            Direction = direction.sqrMagnitude > .0001f
                ? direction.normalized
                : Vector3.zero;
            KnockbackImpulse = Mathf.Max(0f, knockbackImpulse);
        }

        public float Amount { get; }
        public GameObject Instigator { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public float KnockbackImpulse { get; }
    }
}
