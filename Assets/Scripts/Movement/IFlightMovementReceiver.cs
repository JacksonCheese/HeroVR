using UnityEngine;

namespace HeroVR.Movement
{
    public interface IFlightMovementReceiver
    {
        bool IsGrounded { get; }
        Vector3 Velocity { get; }
        Vector3 FlightVelocity { get; }
        Vector3 DesiredWorldMoveDirection { get; }

        void AddFlightImpulse(Vector3 impulse, float maximumSpeed);

        void SetFlightModifiers(
            float gravityScale,
            float liftAcceleration,
            float downwardDamping,
            float maximumClimbSpeed,
            float airSteeringMultiplier,
            float flightDrag,
            float maximumHorizontalSpeed);

        void ResetFlightMotion();
    }
}
