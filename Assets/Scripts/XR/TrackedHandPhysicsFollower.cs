using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.XR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class TrackedHandPhysicsFollower : MonoBehaviour, IHitVelocityProvider
    {
        [SerializeField] private Transform trackingTarget;
        [SerializeField, Min(0f)] private float maximumTrackedSpeed = 20f;

        private Rigidbody body;

        public Transform TrackingTarget => trackingTarget;
        public Vector3 Velocity { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            IgnoreOwnerCollisions();
        }

        private void OnEnable()
        {
            SnapToTarget();
        }

        private void OnDisable()
        {
            Velocity = Vector3.zero;
        }

        public void Configure(Transform target)
        {
            trackingTarget = target;
        }

        private void FixedUpdate()
        {
            if (trackingTarget == null)
            {
                Velocity = Vector3.zero;
                return;
            }

            float deltaTime = Mathf.Max(Time.fixedDeltaTime, .0001f);
            Velocity = Vector3.ClampMagnitude(
                (trackingTarget.position - body.position) / deltaTime,
                maximumTrackedSpeed);

            body.MovePosition(trackingTarget.position);
            body.MoveRotation(trackingTarget.rotation);
        }

        private void SnapToTarget()
        {
            Velocity = Vector3.zero;
            if (body == null || trackingTarget == null)
                return;

            body.position = trackingTarget.position;
            body.rotation = trackingTarget.rotation;
        }

        private void IgnoreOwnerCollisions()
        {
            Damageable owner = GetComponentInParent<Damageable>();
            if (owner == null)
                return;

            Collider[] handColliders = GetComponents<Collider>();
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>(true);
            for (int handIndex = 0; handIndex < handColliders.Length; handIndex++)
            {
                for (int ownerIndex = 0; ownerIndex < ownerColliders.Length; ownerIndex++)
                {
                    Collider ownerCollider = ownerColliders[ownerIndex];
                    if (ownerCollider != handColliders[handIndex])
                        Physics.IgnoreCollision(handColliders[handIndex], ownerCollider);
                }
            }
        }

        private void OnValidate()
        {
            maximumTrackedSpeed = Mathf.Max(0f, maximumTrackedSpeed);
        }
    }
}
