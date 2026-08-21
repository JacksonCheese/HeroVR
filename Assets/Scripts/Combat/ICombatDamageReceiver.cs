namespace HeroVR.Combat
{
    public interface ICombatDamageReceiver
    {
        bool TryReceiveDamage(DamageInfo damageInfo);
    }
}
