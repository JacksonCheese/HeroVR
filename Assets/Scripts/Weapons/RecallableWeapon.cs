using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Weapons
{
    public enum RecallableWeaponState
    {
        Held = 0,
        Thrown = 1,
        Recalling = 2
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class RecallableWeapon : MonoBehaviour, IHitVelocityProvider
    {
        [SerializeField] private Damageable owner;
        [SerializeField] private Transform holdAnchor;
        [SerializeField, Min(0f)] private float throwVelocityMultiplier = 1.35f;
        [SerializeField, Min(0f)] private float maximumThrowSpeed = 24f;
        [SerializeField, Min(0f)] private float recallSpeed = 18f;
        [SerializeField, Min(0f)] private float recallAcceleration = 45f;
        [SerializeField, Min(.01f)] private float catchDistance = .16f;
        [SerializeField, Min(1f)] private float failsafeDistance = 55f;
        [SerializeField] private float failsafeHeight = -25f;
        [SerializeField, Min(0f)] private float throwSpin = 12f;

        private Rigidbody body;
        private Collider[] weaponColliders;
        private float currentRecallSpeed;
        private Vector3 previousHeldPosition;
        private bool ownerDeathSubscribed;

        public RecallableWeaponState State { get; private set; } =
            RecallableWeaponState.Held;
        public Damageable Owner => owner;
        public Transform HoldAnchor => holdAnchor;
        public Vector3 Velocity { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            weaponColliders = GetComponentsInChildren<Collider>(true);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            AttachToHand();
        }

        private void Start()
        {
            ApplyOwnerToHitboxes();
            IgnoreOwnerCollisions();
        }

        private void OnEnable()
        {
            SubscribeToOwnerDeath();
        }

        private void OnDisable()
        {
            UnsubscribeFromOwnerDeath();
        }

        public void ConfigureOwner(Damageable damageOwner)
        {
            UnsubscribeFromOwnerDeath();
            owner = damageOwner;
            ApplyOwnerToHitboxes();
            IgnoreOwnerCollisions();
            SubscribeToOwnerDeath();
        }

        public void SetHoldAnchor(Transform anchor)
        {
            holdAnchor = anchor;
            if (State == RecallableWeaponState.Held)
                AttachToHand();
        }

        public void ConfigureMotion(
            float velocityMultiplier,
            float maxThrowSpeed,
            float returnSpeed,
            float returnAcceleration)
        {
            throwVelocityMultiplier = Mathf.Max(0f, velocityMultiplier);
            maximumThrowSpeed = Mathf.Max(0f, maxThrowSpeed);
            recallSpeed = Mathf.Max(0f, returnSpeed);
            recallAcceleration = Mathf.Max(0f, returnAcceleration);
        }

        public void ConfigureImpact(
            float minimumSpeed,
            float damagePerSpeed,
            float maximumDamage,
            float knockbackMultiplier,
            float maximumKnockback,
            float contactCooldown)
        {
            PunchHitbox[] hitboxes = GetComponentsInChildren<PunchHitbox>(true);
            for (int index = 0; index < hitboxes.Length; index++)
            {
                hitboxes[index].Configure(
                    minimumSpeed,
                    damagePerSpeed,
                    maximumDamage,
                    knockbackMultiplier,
                    maximumKnockback,
                    contactCooldown);
                hitboxes[index].SetOwner(owner);
            }
        }

        public bool TryThrow(Vector3 sourceVelocity)
        {
            if (State != RecallableWeaponState.Held || body == null)
                return false;

            transform.SetParent(null, true);
            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Vector3 throwVelocity = sourceVelocity * throwVelocityMultiplier;
            Velocity = maximumThrowSpeed > 0f
                ? Vector3.ClampMagnitude(throwVelocity, maximumThrowSpeed)
                : throwVelocity;
            body.linearVelocity = Velocity;
            body.angularVelocity = transform.right * throwSpin;
            State = RecallableWeaponState.Thrown;
            return true;
        }

        public bool BeginRecall()
        {
            if (State == RecallableWeaponState.Held || holdAnchor == null || body == null)
                return false;

            transform.SetParent(null, true);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            currentRecallSpeed = 0f;
            State = RecallableWeaponState.Recalling;
            return true;
        }

        public void ForceReturnToHand()
        {
            AttachToHand();
        }

        private void FixedUpdate()
        {
            if (body == null)
                return;

            switch (State)
            {
                case RecallableWeaponState.Held:
                    FollowHand();
                    break;
                case RecallableWeaponState.Thrown:
                    Velocity = body.linearVelocity;
                    CheckFailsafe();
                    break;
                case RecallableWeaponState.Recalling:
                    UpdateRecall();
                    break;
            }
        }

        private void FollowHand()
        {
            if (holdAnchor == null)
            {
                Velocity = Vector3.zero;
                return;
            }

            float deltaTime = Mathf.Max(Time.fixedDeltaTime, .0001f);
            Velocity = (holdAnchor.position - previousHeldPosition) / deltaTime;
            previousHeldPosition = holdAnchor.position;
            body.MovePosition(holdAnchor.position);
            body.MoveRotation(holdAnchor.rotation);
        }

        private void UpdateRecall()
        {
            if (holdAnchor == null)
                return;

            Vector3 toHand = holdAnchor.position - body.position;
            float distance = toHand.magnitude;
            if (distance <= catchDistance)
            {
                AttachToHand();
                return;
            }

            currentRecallSpeed = Mathf.MoveTowards(
                currentRecallSpeed,
                recallSpeed,
                recallAcceleration * Time.fixedDeltaTime);
            Vector3 previousPosition = body.position;
            Vector3 nextPosition = Vector3.MoveTowards(
                previousPosition,
                holdAnchor.position,
                currentRecallSpeed * Time.fixedDeltaTime);
            body.MovePosition(nextPosition);
            body.MoveRotation(Quaternion.RotateTowards(
                body.rotation,
                holdAnchor.rotation,
                720f * Time.fixedDeltaTime));
            Velocity = (nextPosition - previousPosition) /
                Mathf.Max(Time.fixedDeltaTime, .0001f);
        }

        private void CheckFailsafe()
        {
            if (holdAnchor == null)
                return;

            if (transform.position.y < failsafeHeight ||
                Vector3.Distance(transform.position, holdAnchor.position) > failsafeDistance)
            {
                ForceReturnToHand();
            }
        }

        private void AttachToHand()
        {
            if (body == null || holdAnchor == null)
                return;

            State = RecallableWeaponState.Held;
            currentRecallSpeed = 0f;
            Velocity = Vector3.zero;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            transform.SetParent(holdAnchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            body.position = holdAnchor.position;
            body.rotation = holdAnchor.rotation;
            previousHeldPosition = holdAnchor.position;
        }

        private void ApplyOwnerToHitboxes()
        {
            PunchHitbox[] hitboxes = GetComponentsInChildren<PunchHitbox>(true);
            for (int index = 0; index < hitboxes.Length; index++)
                hitboxes[index].SetOwner(owner);
        }

        private void IgnoreOwnerCollisions()
        {
            if (owner == null || weaponColliders == null)
                return;

            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>(true);
            for (int weaponIndex = 0; weaponIndex < weaponColliders.Length; weaponIndex++)
            {
                for (int ownerIndex = 0; ownerIndex < ownerColliders.Length; ownerIndex++)
                {
                    Collider ownerCollider = ownerColliders[ownerIndex];
                    if (ownerCollider != weaponColliders[weaponIndex])
                    {
                        Physics.IgnoreCollision(
                            weaponColliders[weaponIndex],
                            ownerCollider,
                            true);
                    }
                }
            }
        }

        private void SubscribeToOwnerDeath()
        {
            if (!isActiveAndEnabled || owner == null || ownerDeathSubscribed)
                return;

            owner.Died += ForceReturnToHand;
            ownerDeathSubscribed = true;
        }

        private void UnsubscribeFromOwnerDeath()
        {
            if (owner == null || !ownerDeathSubscribed)
                return;

            owner.Died -= ForceReturnToHand;
            ownerDeathSubscribed = false;
        }

        private void OnValidate()
        {
            throwVelocityMultiplier = Mathf.Max(0f, throwVelocityMultiplier);
            maximumThrowSpeed = Mathf.Max(0f, maximumThrowSpeed);
            recallSpeed = Mathf.Max(0f, recallSpeed);
            recallAcceleration = Mathf.Max(0f, recallAcceleration);
            catchDistance = Mathf.Max(.01f, catchDistance);
            failsafeDistance = Mathf.Max(1f, failsafeDistance);
            throwSpin = Mathf.Max(0f, throwSpin);
        }
    }
}
