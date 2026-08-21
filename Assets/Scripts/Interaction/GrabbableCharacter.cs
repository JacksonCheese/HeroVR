using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable), typeof(RagdollController))]
    public sealed class GrabbableCharacter : MonoBehaviour, IPhysicsGrabbable
    {
        [SerializeField] private bool allowGrab = true;
        [SerializeField] private bool allowRootColliderGrab = true;
        [SerializeField] private bool grabbableWhenDead = true;
        [SerializeField, Min(0f)] private float throwVelocityMultiplier = 1f;
        [SerializeField, Min(0f)] private float maximumThrowSpeed = 24f;

        private Damageable damageable;
        private RagdollController ragdoll;
        private PhysicsGrabInteractor currentInteractor;
        private Rigidbody grabbedBody;
        private ImpactDamageDealer impactDealer;

        public bool IsGrabbed => currentInteractor != null;
        public Rigidbody GrabBody => grabbedBody;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
            ragdoll = GetComponent<RagdollController>();
        }

        public void Configure(
            bool canGrab,
            bool allowRootGrab,
            bool allowDeadGrab,
            float velocityMultiplier,
            float maxThrowSpeed)
        {
            allowGrab = canGrab;
            allowRootColliderGrab = allowRootGrab;
            grabbableWhenDead = allowDeadGrab;
            throwVelocityMultiplier = Mathf.Max(0f, velocityMultiplier);
            maximumThrowSpeed = Mathf.Max(0f, maxThrowSpeed);
        }

        public bool CanGrab(PhysicsGrabInteractor interactor, Collider hitCollider)
        {
            if (!allowGrab || interactor == null || currentInteractor != null ||
                (damageable.IsDead && !grabbableWhenDead))
            {
                return false;
            }

            CharacterGrabArea area =
                hitCollider != null ? hitCollider.GetComponent<CharacterGrabArea>() : null;
            return area != null && area.Character == this || allowRootColliderGrab;
        }

        public bool TryBeginGrab(
            PhysicsGrabInteractor interactor,
            Collider hitCollider)
        {
            if (!CanGrab(interactor, hitCollider))
                return false;

            ragdoll.ForceRagdoll(new DamageInfo(
                0f,
                interactor.Instigator,
                hitCollider != null ? hitCollider.ClosestPoint(interactor.transform.position) :
                    transform.position,
                Vector3.zero,
                0f,
                ragdoll.ActivationImpactStrength,
                DamageType.HeavyPhysical));

            CharacterGrabArea area =
                hitCollider != null ? hitCollider.GetComponent<CharacterGrabArea>() : null;
            grabbedBody = area != null && area.PreferredBody != null
                ? area.PreferredBody
                : ragdoll.GetClosestBody(
                    hitCollider != null ? hitCollider.bounds.center : transform.position);
            if (grabbedBody == null)
                return false;

            impactDealer = grabbedBody.GetComponent<ImpactDamageDealer>();
            if (impactDealer == null)
                impactDealer = grabbedBody.gameObject.AddComponent<ImpactDamageDealer>();

            currentInteractor = interactor;
            impactDealer.SetInstigator(interactor.Instigator);
            return true;
        }

        public void EndGrab(
            PhysicsGrabInteractor interactor,
            Vector3 throwVelocity)
        {
            if (interactor == null || currentInteractor != interactor)
                return;

            currentInteractor = null;
            Vector3 inheritedVelocity = Vector3.ClampMagnitude(
                throwVelocity * throwVelocityMultiplier,
                maximumThrowSpeed);
            if (grabbedBody != null)
            {
                grabbedBody.isKinematic = false;
                grabbedBody.WakeUp();
                grabbedBody.linearVelocity = inheritedVelocity;
            }

            if (impactDealer != null)
                impactDealer.SetInstigator(interactor.Instigator);
            grabbedBody = null;
        }

        private void OnValidate()
        {
            throwVelocityMultiplier = Mathf.Max(0f, throwVelocityMultiplier);
            maximumThrowSpeed = Mathf.Max(0f, maximumThrowSpeed);
        }
    }
}
