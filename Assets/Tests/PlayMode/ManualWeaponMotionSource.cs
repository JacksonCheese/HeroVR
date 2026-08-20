using HeroVR.Weapons;
using UnityEngine;

namespace HeroVR.Tests
{
    public sealed class ManualWeaponMotionSource : MonoBehaviour,
        IWeaponMotionSource
    {
        public WeaponMotionSample CurrentMotion { get; private set; }

        public void SetMotion(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            bool isHeld,
            GameObject owner)
        {
            CurrentMotion = new WeaponMotionSample(
                linearVelocity,
                angularVelocity,
                isHeld,
                owner);
        }
    }
}
