using UnityEngine;

namespace HeroVR.Combat
{
    /// <summary>
    /// Supplies world-space velocity to hitboxes whose Rigidbody velocity is not
    /// representative, such as kinematic XR hand proxies.
    /// </summary>
    public interface IHitVelocityProvider
    {
        Vector3 Velocity { get; }
    }
}
