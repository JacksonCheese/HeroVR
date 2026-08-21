using System;
using System.Collections.Generic;
using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class PhysicsGrabInteractor : MonoBehaviour
    {
        [SerializeField, Min(.01f)] private float grabRadius = .3f;
        [SerializeField] private LayerMask grabLayers = ~0;
        [SerializeField] private MonoBehaviour velocityProviderSource;
        [SerializeField] private GameObject instigator;
        [SerializeField, Min(0f)] private float throwVelocityMultiplier = 1f;
        [SerializeField, Min(0f)] private float maximumThrowSpeed = 24f;

        private readonly Collider[] overlapBuffer = new Collider[24];
        private readonly List<Collider> ignoredTargetColliders = new List<Collider>();
        private Rigidbody body;
        private Collider[] handColliders;
        private IHitVelocityProvider velocityProvider;
        private ConfigurableJoint grabJoint;
        private IPhysicsGrabbable heldTarget;

        public bool IsHolding => heldTarget != null;
        public IPhysicsGrabbable HeldTarget => heldTarget;
        public GameObject Instigator => instigator != null
            ? instigator
            : transform.root.gameObject;

        public event Action<IPhysicsGrabbable> Grabbed;
        public event Action<IPhysicsGrabbable> Released;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            handColliders = GetComponents<Collider>();
            velocityProvider = velocityProviderSource as IHitVelocityProvider;
        }

        private void OnDisable()
        {
            if (heldTarget != null)
                Release(Vector3.zero);
        }

        public void Configure(
            MonoBehaviour trackedVelocityProvider,
            GameObject damageInstigator,
            float radius,
            float velocityMultiplier,
            float maxThrowSpeed)
        {
            velocityProviderSource = trackedVelocityProvider;
            velocityProvider = trackedVelocityProvider as IHitVelocityProvider;
            instigator = damageInstigator;
            grabRadius = Mathf.Max(.01f, radius);
            throwVelocityMultiplier = Mathf.Max(0f, velocityMultiplier);
            maximumThrowSpeed = Mathf.Max(0f, maxThrowSpeed);
        }

        public bool TryBeginGrab()
        {
            return TryBeginGrab(transform.position);
        }

        public bool TryBeginGrab(Vector3 worldCenter)
        {
            if (heldTarget != null)
                return false;

            int hitCount = Physics.OverlapSphereNonAlloc(
                worldCenter,
                grabRadius,
                overlapBuffer,
                grabLayers,
                QueryTriggerInteraction.Collide);

            IPhysicsGrabbable bestTarget = null;
            Collider bestCollider = null;
            float bestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                Collider candidateCollider = overlapBuffer[index];
                if (candidateCollider == null || IsOwnCollider(candidateCollider))
                    continue;

                IPhysicsGrabbable candidate = FindGrabbable(candidateCollider.transform);
                if (candidate == null || !candidate.CanGrab(this, candidateCollider))
                    continue;

                float distance = candidateCollider.bounds.SqrDistance(worldCenter);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = candidate;
                    bestCollider = candidateCollider;
                }
            }

            if (bestTarget == null || !bestTarget.TryBeginGrab(this, bestCollider))
                return false;

            Rigidbody targetBody = bestTarget.GrabBody;
            if (targetBody == null)
            {
                bestTarget.EndGrab(this, Vector3.zero);
                return false;
            }

            heldTarget = bestTarget;
            CreateGrabJoint(targetBody);
            IgnoreTargetCollisions(targetBody.transform.root, true);
            Grabbed?.Invoke(bestTarget);
            return true;
        }

        public bool Release()
        {
            Vector3 trackedVelocity = velocityProvider != null
                ? velocityProvider.Velocity
                : body.linearVelocity;
            Vector3 throwVelocity = Vector3.ClampMagnitude(
                trackedVelocity * throwVelocityMultiplier,
                maximumThrowSpeed);
            return Release(throwVelocity);
        }

        public bool Release(Vector3 throwVelocity)
        {
            if (heldTarget == null)
                return false;

            IPhysicsGrabbable releasedTarget = heldTarget;
            heldTarget = null;
            DestroyGrabJoint();
            RestoreIgnoredCollisions();
            releasedTarget.EndGrab(
                this,
                Vector3.ClampMagnitude(throwVelocity, maximumThrowSpeed));
            Released?.Invoke(releasedTarget);
            return true;
        }

        private void CreateGrabJoint(Rigidbody connectedBody)
        {
            grabJoint = gameObject.AddComponent<ConfigurableJoint>();
            grabJoint.connectedBody = connectedBody;
            grabJoint.autoConfigureConnectedAnchor = false;
            grabJoint.anchor = Vector3.zero;
            grabJoint.connectedAnchor = connectedBody.transform.InverseTransformPoint(
                transform.position);
            grabJoint.xMotion = ConfigurableJointMotion.Locked;
            grabJoint.yMotion = ConfigurableJointMotion.Locked;
            grabJoint.zMotion = ConfigurableJointMotion.Locked;
            grabJoint.angularXMotion = ConfigurableJointMotion.Locked;
            grabJoint.angularYMotion = ConfigurableJointMotion.Locked;
            grabJoint.angularZMotion = ConfigurableJointMotion.Locked;
            grabJoint.enableCollision = false;
            grabJoint.enablePreprocessing = false;
            grabJoint.projectionMode = JointProjectionMode.PositionAndRotation;
            grabJoint.projectionDistance = .08f;
            grabJoint.projectionAngle = 12f;
        }

        private void DestroyGrabJoint()
        {
            if (grabJoint == null)
                return;

            grabJoint.connectedBody = null;
            Destroy(grabJoint);
            grabJoint = null;
        }

        private void IgnoreTargetCollisions(Transform targetRoot, bool ignore)
        {
            ignoredTargetColliders.Clear();
            Collider[] targetColliders = targetRoot.GetComponentsInChildren<Collider>(true);
            for (int handIndex = 0; handIndex < handColliders.Length; handIndex++)
            {
                for (int targetIndex = 0; targetIndex < targetColliders.Length; targetIndex++)
                {
                    Collider targetCollider = targetColliders[targetIndex];
                    if (targetCollider == handColliders[handIndex])
                        continue;

                    Physics.IgnoreCollision(handColliders[handIndex], targetCollider, ignore);
                    if (!ignoredTargetColliders.Contains(targetCollider))
                        ignoredTargetColliders.Add(targetCollider);
                }
            }
        }

        private void RestoreIgnoredCollisions()
        {
            for (int handIndex = 0; handIndex < handColliders.Length; handIndex++)
            {
                for (int targetIndex = 0; targetIndex < ignoredTargetColliders.Count; targetIndex++)
                {
                    Collider targetCollider = ignoredTargetColliders[targetIndex];
                    if (targetCollider != null)
                    {
                        Physics.IgnoreCollision(
                            handColliders[handIndex],
                            targetCollider,
                            false);
                    }
                }
            }

            ignoredTargetColliders.Clear();
        }

        private bool IsOwnCollider(Collider candidate)
        {
            return candidate.transform.root == transform.root;
        }

        private static IPhysicsGrabbable FindGrabbable(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is IPhysicsGrabbable grabbable)
                        return grabbable;
                }

                current = current.parent;
            }

            return null;
        }

        private void OnValidate()
        {
            grabRadius = Mathf.Max(.01f, grabRadius);
            throwVelocityMultiplier = Mathf.Max(0f, throwVelocityMultiplier);
            maximumThrowSpeed = Mathf.Max(0f, maximumThrowSpeed);
        }
    }
}
