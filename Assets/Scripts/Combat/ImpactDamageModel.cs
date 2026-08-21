using UnityEngine;

namespace HeroVR.Combat
{
    public static class ImpactDamageModel
    {
        public static ImpactDamageResult Calculate(
            float mass,
            float relativeSpeed,
            float collisionImpulse,
            float minimumSpeed,
            float minimumMomentum,
            float damagePerMomentum,
            float maximumDamage,
            float impulseToKnockback,
            float maximumKnockback,
            float impactStrengthScale)
        {
            float safeMass = Mathf.Max(.01f, mass);
            float speed = Mathf.Max(0f, relativeSpeed);
            float momentum = safeMass * speed;
            float strength = Mathf.Max(momentum, Mathf.Max(0f, collisionImpulse)) *
                Mathf.Max(0f, impactStrengthScale);

            if (speed < Mathf.Max(0f, minimumSpeed) ||
                momentum < Mathf.Max(0f, minimumMomentum))
            {
                return new ImpactDamageResult(speed, momentum, strength, 0f, 0f);
            }

            float damage = Mathf.Min(
                Mathf.Max(0f, momentum - minimumMomentum) *
                    Mathf.Max(0f, damagePerMomentum),
                Mathf.Max(0f, maximumDamage));
            float knockback = Mathf.Min(
                strength * Mathf.Max(0f, impulseToKnockback),
                Mathf.Max(0f, maximumKnockback));
            return new ImpactDamageResult(
                speed,
                momentum,
                strength,
                damage,
                knockback);
        }
    }
}
