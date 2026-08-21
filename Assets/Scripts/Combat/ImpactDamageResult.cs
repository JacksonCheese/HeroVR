namespace HeroVR.Combat
{
    public readonly struct ImpactDamageResult
    {
        public ImpactDamageResult(
            float speed,
            float momentum,
            float impactStrength,
            float damage,
            float knockbackImpulse)
        {
            Speed = speed;
            Momentum = momentum;
            ImpactStrength = impactStrength;
            Damage = damage;
            KnockbackImpulse = knockbackImpulse;
        }

        public float Speed { get; }
        public float Momentum { get; }
        public float ImpactStrength { get; }
        public float Damage { get; }
        public float KnockbackImpulse { get; }
        public bool IsDamaging => Damage > 0f;
    }
}
