using UnityEngine;

namespace HeroVR.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(ThrowableObject))]
    public sealed class GrabbableObject : MonoBehaviour, IPhysicsGrabbable
    {
        [SerializeField] private bool allowGrab = true;

        private Rigidbody body;
        private ThrowableObject throwable;
        private PhysicsGrabInteractor currentInteractor;

        public bool IsGrabbed => currentInteractor != null;
        public Rigidbody GrabBody => body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            throwable = GetComponent<ThrowableObject>();
        }

        public bool CanGrab(PhysicsGrabInteractor interactor, Collider hitCollider)
        {
            return allowGrab && interactor != null && currentInteractor == null;
        }

        public bool TryBeginGrab(
            PhysicsGrabInteractor interactor,
            Collider hitCollider)
        {
            if (!CanGrab(interactor, hitCollider))
                return false;

            currentInteractor = interactor;
            body.isKinematic = false;
            body.WakeUp();
            return true;
        }

        public void EndGrab(
            PhysicsGrabInteractor interactor,
            Vector3 throwVelocity)
        {
            if (interactor == null || currentInteractor != interactor)
                return;

            currentInteractor = null;
            throwable.Throw(interactor.Instigator, throwVelocity);
        }
    }
}
