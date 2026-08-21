namespace HeroVR.Combat
{
    public readonly struct CombatImpact
    {
        public CombatImpact(
            Damageable target,
            DamageInfo damageInfo,
            float appliedDamage)
        {
            Target = target;
            DamageInfo = damageInfo;
            AppliedDamage = appliedDamage;
        }

        public Damageable Target { get; }
        public DamageInfo DamageInfo { get; }
        public float AppliedDamage { get; }
        public float ImpactStrength => DamageInfo.ImpactStrength;
        public ImpactSeverity Severity => DamageInfo.Severity;
    }
}
