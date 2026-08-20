using UnityEngine;

namespace HeroVR.Abilities
{
    public interface IAimProvider
    {
        Vector3 Origin { get; }
        Vector3 Direction { get; }
        Vector3 Up { get; }
    }
}
