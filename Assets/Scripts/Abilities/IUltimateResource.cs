namespace HeroVR.Abilities
{
    public interface IUltimateResource
    {
        bool IsUltimateReady { get; }
        bool TryConsumeUltimate();
    }
}
