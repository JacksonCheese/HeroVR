using UnityEngine;

namespace HeroVR.Gameplay
{
    public interface IControlSuspendable
    {
        bool IsControlSuspended { get; }
        void SetControlSuspended(Object source, bool suspended);
    }
}
