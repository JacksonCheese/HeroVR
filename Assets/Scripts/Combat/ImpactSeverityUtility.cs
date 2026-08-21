namespace HeroVR.Combat
{
    public static class ImpactSeverityUtility
    {
        public const float MediumThreshold = 8f;
        public const float HeavyThreshold = 18f;
        public const float ExtremeThreshold = 35f;

        public static ImpactSeverity Classify(float strength)
        {
            if (strength >= ExtremeThreshold)
                return ImpactSeverity.Extreme;
            if (strength >= HeavyThreshold)
                return ImpactSeverity.Heavy;
            if (strength >= MediumThreshold)
                return ImpactSeverity.Medium;
            return ImpactSeverity.Light;
        }

        public static DamageType PhysicalDamageType(float strength)
        {
            return strength >= HeavyThreshold
                ? DamageType.HeavyPhysical
                : DamageType.Physical;
        }
    }
}
