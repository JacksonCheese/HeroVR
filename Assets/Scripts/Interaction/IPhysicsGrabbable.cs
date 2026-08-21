using UnityEngine;

namespace HeroVR.Interaction
{
    public interface IPhysicsGrabbable
    {
        bool IsGrabbed { get; }
        Rigidbody GrabBody { get; }
        bool CanGrab(PhysicsGrabInteractor interactor, Collider hitCollider);
        bool TryBeginGrab(PhysicsGrabInteractor interactor, Collider hitCollider);
        void EndGrab(PhysicsGrabInteractor interactor, Vector3 throwVelocity);
    }
}
