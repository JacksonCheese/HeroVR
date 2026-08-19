namespace HeroVR.Combat
{
    public interface IDamageDealtReceiver
    {
        void OnDamageDealt(
            Damageable target,
            DamageInfo damageInfo,
            float appliedDamage);
    }
}
