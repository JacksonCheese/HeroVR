using UnityEngine;

namespace HeroVR.Heroes
{
    [CreateAssetMenu(
        fileName = "ThorHammerFlightSettings",
        menuName = "HeroVR/Thor/Hammer Flight Settings")]
    public sealed class ThorHammerFlightSettings : ScriptableObject
    {
        [Header("Spin Charge")]
        [SerializeField, Min(0f)] private float minimumSpinSpeed = 12f;
        [SerializeField, Min(0f)] private float requiredSpinDuration = .45f;
        [SerializeField, Min(0f)] private float spinChargeDecayRate = 1.5f;
        [SerializeField, Min(0f)] private float launchSpinGracePeriod = .3f;

        [Header("Launch")]
        [SerializeField, Min(0f)] private float launchMotionThreshold = 5.5f;
        [SerializeField, Min(0f)] private float launchImpulse = 11f;
        [SerializeField, Min(0f)] private float groundedLaunchLift = 3f;
        [SerializeField, Min(0f)] private float maximumFlightSpeed = 15f;
        [SerializeField, Min(0f)] private float launchCooldown = .55f;

        [Header("Hover")]
        [SerializeField, Min(0f)] private float hoverActivationSpinSpeed = 12f;
        [SerializeField, Min(0f)] private float hoverDeactivationSpinSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float hoverGravityMultiplier = .2f;
        [SerializeField, Min(0f)] private float hoverLiftAcceleration = 4.2f;
        [SerializeField, Min(0f)] private float hoverDownwardDamping = 6f;
        [SerializeField, Min(0f)] private float maximumClimbSpeed = 4f;
        [SerializeField, Min(0f)] private float gravityRestorationRate = 2.5f;

        [Header("Air Control")]
        [SerializeField, Range(0f, 1f)] private float airSteeringMultiplier = .45f;
        [SerializeField, Min(0f)] private float flightDrag = 1.2f;
        [SerializeField, Min(0f)] private float maximumHorizontalSpeed = 15f;

        public float MinimumSpinSpeed => minimumSpinSpeed;
        public float RequiredSpinDuration => requiredSpinDuration;
        public float SpinChargeDecayRate => spinChargeDecayRate;
        public float LaunchSpinGracePeriod => launchSpinGracePeriod;
        public float LaunchMotionThreshold => launchMotionThreshold;
        public float LaunchImpulse => launchImpulse;
        public float GroundedLaunchLift => groundedLaunchLift;
        public float MaximumFlightSpeed => maximumFlightSpeed;
        public float LaunchCooldown => launchCooldown;
        public float HoverActivationSpinSpeed => hoverActivationSpinSpeed;
        public float HoverDeactivationSpinSpeed => hoverDeactivationSpinSpeed;
        public float HoverGravityMultiplier => hoverGravityMultiplier;
        public float HoverLiftAcceleration => hoverLiftAcceleration;
        public float HoverDownwardDamping => hoverDownwardDamping;
        public float MaximumClimbSpeed => maximumClimbSpeed;
        public float GravityRestorationRate => gravityRestorationRate;
        public float AirSteeringMultiplier => airSteeringMultiplier;
        public float FlightDrag => flightDrag;
        public float MaximumHorizontalSpeed => maximumHorizontalSpeed;

        public void Configure(
            float spinSpeed,
            float spinDuration,
            float chargeDecayRate,
            float spinGracePeriod,
            float motionThreshold,
            float impulse,
            float launchLift,
            float maximumSpeed,
            float cooldown,
            float hoverActivationSpeed,
            float hoverDeactivationSpeed,
            float gravityMultiplier,
            float liftAcceleration,
            float downwardDamping,
            float climbSpeed,
            float gravityRestoreRate,
            float steeringMultiplier,
            float drag,
            float horizontalSpeed)
        {
            minimumSpinSpeed = Mathf.Max(0f, spinSpeed);
            requiredSpinDuration = Mathf.Max(0f, spinDuration);
            spinChargeDecayRate = Mathf.Max(0f, chargeDecayRate);
            launchSpinGracePeriod = Mathf.Max(0f, spinGracePeriod);
            launchMotionThreshold = Mathf.Max(0f, motionThreshold);
            launchImpulse = Mathf.Max(0f, impulse);
            groundedLaunchLift = Mathf.Max(0f, launchLift);
            maximumFlightSpeed = Mathf.Max(0f, maximumSpeed);
            launchCooldown = Mathf.Max(0f, cooldown);
            hoverActivationSpinSpeed = Mathf.Max(0f, hoverActivationSpeed);
            hoverDeactivationSpinSpeed = Mathf.Clamp(
                hoverDeactivationSpeed,
                0f,
                hoverActivationSpinSpeed);
            hoverGravityMultiplier = Mathf.Clamp01(gravityMultiplier);
            hoverLiftAcceleration = Mathf.Max(0f, liftAcceleration);
            hoverDownwardDamping = Mathf.Max(0f, downwardDamping);
            maximumClimbSpeed = Mathf.Max(0f, climbSpeed);
            gravityRestorationRate = Mathf.Max(0f, gravityRestoreRate);
            airSteeringMultiplier = Mathf.Clamp01(steeringMultiplier);
            flightDrag = Mathf.Max(0f, drag);
            maximumHorizontalSpeed = Mathf.Max(0f, horizontalSpeed);
        }

        private void OnValidate()
        {
            minimumSpinSpeed = Mathf.Max(0f, minimumSpinSpeed);
            requiredSpinDuration = Mathf.Max(0f, requiredSpinDuration);
            spinChargeDecayRate = Mathf.Max(0f, spinChargeDecayRate);
            launchSpinGracePeriod = Mathf.Max(0f, launchSpinGracePeriod);
            launchMotionThreshold = Mathf.Max(0f, launchMotionThreshold);
            launchImpulse = Mathf.Max(0f, launchImpulse);
            groundedLaunchLift = Mathf.Max(0f, groundedLaunchLift);
            maximumFlightSpeed = Mathf.Max(0f, maximumFlightSpeed);
            launchCooldown = Mathf.Max(0f, launchCooldown);
            hoverActivationSpinSpeed = Mathf.Max(0f, hoverActivationSpinSpeed);
            hoverDeactivationSpinSpeed = Mathf.Clamp(
                hoverDeactivationSpinSpeed,
                0f,
                hoverActivationSpinSpeed);
            hoverGravityMultiplier = Mathf.Clamp01(hoverGravityMultiplier);
            hoverLiftAcceleration = Mathf.Max(0f, hoverLiftAcceleration);
            hoverDownwardDamping = Mathf.Max(0f, hoverDownwardDamping);
            maximumClimbSpeed = Mathf.Max(0f, maximumClimbSpeed);
            gravityRestorationRate = Mathf.Max(0f, gravityRestorationRate);
            airSteeringMultiplier = Mathf.Clamp01(airSteeringMultiplier);
            flightDrag = Mathf.Max(0f, flightDrag);
            maximumHorizontalSpeed = Mathf.Max(0f, maximumHorizontalSpeed);
        }
    }
}
