using UnityEngine;

namespace HeroVR.Weapons
{
    [DefaultExecutionOrder(-30)]
    [DisallowMultipleComponent]
    public sealed class TransformWeaponMotionSource : MonoBehaviour, IWeaponMotionSource
    {
        [SerializeField] private Transform motionTransform;
        [SerializeField] private RecallableWeapon weapon;
        [SerializeField] private MonoBehaviour holdStateSource;
        [SerializeField, Min(0f)] private float smoothingTime = .12f;

        private IWeaponHoldStateSource holdState;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 smoothedLinearVelocity;
        private Vector3 smoothedAngularVelocity;
        private bool hasPreviousPose;

        public WeaponMotionSample CurrentMotion { get; private set; }
        public float SmoothingTime => smoothingTime;

        private void Awake()
        {
            CacheHoldState();
            ResetSamples();
        }

        private void OnEnable()
        {
            ResetSamples();
        }

        private void OnDisable()
        {
            CurrentMotion = default;
            hasPreviousPose = false;
        }

        public void Configure(
            Transform sourceTransform,
            RecallableWeapon recallableWeapon,
            MonoBehaviour weaponHoldSource,
            float velocitySmoothingTime)
        {
            motionTransform = sourceTransform;
            weapon = recallableWeapon;
            holdStateSource = weaponHoldSource;
            smoothingTime = Mathf.Max(0f, velocitySmoothingTime);
            CacheHoldState();
            ResetSamples();
        }

        private void FixedUpdate()
        {
            if (motionTransform == null)
            {
                CurrentMotion = default;
                hasPreviousPose = false;
                return;
            }

            float deltaTime = Mathf.Max(Time.fixedDeltaTime, .0001f);
            Transform reference = weapon != null && weapon.Owner != null
                ? weapon.Owner.transform
                : null;
            Vector3 position = reference != null
                ? reference.InverseTransformPoint(motionTransform.position)
                : motionTransform.position;
            Quaternion rotation = reference != null
                ? Quaternion.Inverse(reference.rotation) * motionTransform.rotation
                : motionTransform.rotation;
            if (!hasPreviousPose)
            {
                previousPosition = position;
                previousRotation = rotation;
                hasPreviousPose = true;
            }

            Vector3 relativeLinearVelocity =
                (position - previousPosition) / deltaTime;
            Vector3 relativeAngularVelocity = CalculateAngularVelocity(
                previousRotation,
                rotation,
                deltaTime);
            Vector3 linearVelocity = reference != null
                ? reference.TransformDirection(relativeLinearVelocity)
                : relativeLinearVelocity;
            Vector3 angularVelocity = reference != null
                ? reference.TransformDirection(relativeAngularVelocity)
                : relativeAngularVelocity;
            float blend = smoothingTime <= 0f
                ? 1f
                : 1f - Mathf.Exp(-deltaTime / smoothingTime);
            smoothedLinearVelocity = Vector3.Lerp(
                smoothedLinearVelocity,
                linearVelocity,
                blend);
            smoothedAngularVelocity = Vector3.Lerp(
                smoothedAngularVelocity,
                angularVelocity,
                blend);

            bool isHeld = weapon != null &&
                weapon.State == RecallableWeaponState.Held &&
                (holdState == null || holdState.IsWeaponHeld);
            GameObject owner = weapon != null && weapon.Owner != null
                ? weapon.Owner.gameObject
                : null;
            CurrentMotion = new WeaponMotionSample(
                smoothedLinearVelocity,
                smoothedAngularVelocity,
                isHeld,
                owner);

            previousPosition = position;
            previousRotation = rotation;
        }

        private void CacheHoldState()
        {
            holdState = holdStateSource as IWeaponHoldStateSource;
        }

        private void ResetSamples()
        {
            smoothedLinearVelocity = Vector3.zero;
            smoothedAngularVelocity = Vector3.zero;
            CurrentMotion = default;
            hasPreviousPose = false;
            if (motionTransform == null)
                return;

            Transform reference = weapon != null && weapon.Owner != null
                ? weapon.Owner.transform
                : null;
            previousPosition = reference != null
                ? reference.InverseTransformPoint(motionTransform.position)
                : motionTransform.position;
            previousRotation = reference != null
                ? Quaternion.Inverse(reference.rotation) * motionTransform.rotation
                : motionTransform.rotation;
            hasPreviousPose = true;
        }

        private static Vector3 CalculateAngularVelocity(
            Quaternion previous,
            Quaternion current,
            float deltaTime)
        {
            Quaternion delta = current * Quaternion.Inverse(previous);
            delta.ToAngleAxis(out float angleDegrees, out Vector3 axis);
            if (angleDegrees > 180f)
                angleDegrees -= 360f;

            if (axis.sqrMagnitude <= .0001f || Mathf.Abs(angleDegrees) <= .0001f)
                return Vector3.zero;

            return axis.normalized * angleDegrees * Mathf.Deg2Rad / deltaTime;
        }

        private void OnValidate()
        {
            smoothingTime = Mathf.Max(0f, smoothingTime);
        }
    }
}
