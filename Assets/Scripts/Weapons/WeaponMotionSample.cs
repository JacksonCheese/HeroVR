using UnityEngine;

namespace HeroVR.Weapons
{
    public readonly struct WeaponMotionSample
    {
        public WeaponMotionSample(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            bool isHeld,
            GameObject owner)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            IsHeld = isHeld;
            Owner = owner;
        }

        public Vector3 LinearVelocity { get; }
        public Vector3 AngularVelocity { get; }
        public float SpinMagnitude => AngularVelocity.magnitude;
        public bool IsHeld { get; }
        public GameObject Owner { get; }
    }
}
