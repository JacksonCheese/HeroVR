using HeroVR.Combat;
using HeroVR.Movement;
using HeroVR.Weapons;
using UnityEngine;

namespace HeroVR.Heroes
{
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable), typeof(RespawnOnDeath))]
    public sealed class ThorHammerFlight : MonoBehaviour
    {
        [SerializeField] private ThorHammerFlightSettings settings;
        [SerializeField] private RecallableWeapon weapon;
        [SerializeField] private MonoBehaviour motionSourceComponent;
        [SerializeField] private MonoBehaviour movementReceiverComponent;

        private Damageable health;
        private RespawnOnDeath respawn;
        private IWeaponMotionSource motionSource;
        private IFlightMovementReceiver movementReceiver;
        private float spinChargeTime;
        private float launchGraceRemaining;
        private float launchCooldownRemaining;
        private float currentGravityScale = 1f;
        private bool launchMotionLatched;
        private bool launchedThisStep;

        public bool IsSpinCharged => settings != null &&
            spinChargeTime >= settings.RequiredSpinDuration;
        public bool IsHovering { get; private set; }
        public bool IsFlightActive { get; private set; }
        public float SpinChargeTime => spinChargeTime;
        public float SpinMagnitude { get; private set; }
        public float CurrentGravityScale => currentGravityScale;
        public Vector3 LastLaunchDirection { get; private set; }
        public int LaunchCount { get; private set; }
        public ThorHammerFlightSettings Settings => settings;
        public RecallableWeapon Weapon => weapon;
        public Vector3 MovementVelocity => movementReceiver != null
            ? movementReceiver.Velocity
            : Vector3.zero;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            respawn = GetComponent<RespawnOnDeath>();
            CacheAdapters();
        }

        private void OnEnable()
        {
            if (health != null)
                health.Died += OnOwnerDied;
            if (respawn != null)
                respawn.Respawned += OnOwnerRespawned;
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= OnOwnerDied;
            if (respawn != null)
                respawn.Respawned -= OnOwnerRespawned;
            ResetFlight(false);
        }

        public void Configure(
            ThorHammerFlightSettings flightSettings,
            RecallableWeapon recallableWeapon,
            MonoBehaviour weaponMotionSource,
            MonoBehaviour flightMovementReceiver)
        {
            settings = flightSettings;
            weapon = recallableWeapon;
            motionSourceComponent = weaponMotionSource;
            movementReceiverComponent = flightMovementReceiver;
            CacheAdapters();
        }

        private void Update()
        {
            EvaluateMotion(Time.deltaTime);
        }

        public void EvaluateMotion(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);
            launchedThisStep = false;
            launchCooldownRemaining = Mathf.Max(
                0f,
                launchCooldownRemaining - deltaTime);

            if (settings == null || motionSource == null || movementReceiver == null ||
                health == null || health.IsDead)
            {
                StopFlightSupport(deltaTime);
                return;
            }

            WeaponMotionSample motion = motionSource.CurrentMotion;
            SpinMagnitude = motion.SpinMagnitude;
            bool validHeldMotion = IsValidHeldMotion(motion);
            UpdateSpinCharge(validHeldMotion, deltaTime);
            TryLaunch(motion, validHeldMotion);
            UpdateHover(validHeldMotion);
            UpdateMovementSupport(deltaTime);

            if (movementReceiver.IsGrounded && !launchedThisStep)
            {
                IsFlightActive = false;
                if (!IsHovering)
                    currentGravityScale = 1f;
            }
            else if (movementReceiver.FlightVelocity.sqrMagnitude > .01f || IsHovering)
            {
                IsFlightActive = true;
            }
        }

        public void ResetFlight(bool returnWeaponToHand)
        {
            spinChargeTime = 0f;
            launchGraceRemaining = 0f;
            launchCooldownRemaining = 0f;
            currentGravityScale = 1f;
            launchMotionLatched = false;
            launchedThisStep = false;
            SpinMagnitude = 0f;
            IsHovering = false;
            IsFlightActive = false;
            LastLaunchDirection = Vector3.zero;
            movementReceiver?.ResetFlightMotion();

            if (returnWeaponToHand && weapon != null)
                weapon.ForceReturnToHand();
        }

        private void CacheAdapters()
        {
            motionSource = motionSourceComponent as IWeaponMotionSource;
            movementReceiver = movementReceiverComponent as IFlightMovementReceiver;
        }

        private bool IsValidHeldMotion(WeaponMotionSample motion)
        {
            if (!motion.IsHeld || weapon == null ||
                weapon.State != RecallableWeaponState.Held ||
                weapon.Owner != health || motion.Owner == null)
            {
                return false;
            }

            return motion.Owner.transform.root == health.transform.root;
        }

        private void UpdateSpinCharge(bool validHeldMotion, float deltaTime)
        {
            bool aboveChargeThreshold = validHeldMotion &&
                SpinMagnitude >= settings.MinimumSpinSpeed;
            if (aboveChargeThreshold)
            {
                spinChargeTime = Mathf.Min(
                    settings.RequiredSpinDuration,
                    spinChargeTime + deltaTime);
            }
            else
            {
                spinChargeTime = Mathf.Max(
                    0f,
                    spinChargeTime - settings.SpinChargeDecayRate * deltaTime);
            }

            if (IsSpinCharged)
                launchGraceRemaining = settings.LaunchSpinGracePeriod;
            else
                launchGraceRemaining = Mathf.Max(0f, launchGraceRemaining - deltaTime);
        }

        private void TryLaunch(WeaponMotionSample motion, bool validHeldMotion)
        {
            float launchSpeed = motion.LinearVelocity.magnitude;
            float resetThreshold = settings.LaunchMotionThreshold * .6f;
            if (launchSpeed < resetThreshold)
            {
                launchMotionLatched = false;
                return;
            }

            if (launchSpeed < settings.LaunchMotionThreshold || launchMotionLatched)
                return;

            // Latch every strong motion, including uncharged ones. This requires a
            // distinct directional swing after charging rather than allowing the
            // circular spin motion itself to become a launch one frame later.
            launchMotionLatched = true;
            bool hasSpinCharge = IsSpinCharged || launchGraceRemaining > 0f;
            if (!validHeldMotion || !hasSpinCharge || launchCooldownRemaining > 0f)
                return;

            Vector3 direction = motion.LinearVelocity.normalized;
            Vector3 impulse = direction * settings.LaunchImpulse;
            if (movementReceiver.IsGrounded && settings.GroundedLaunchLift > 0f)
                impulse += transform.up * settings.GroundedLaunchLift;
            movementReceiver.AddFlightImpulse(
                impulse,
                settings.MaximumFlightSpeed);
            LastLaunchDirection = direction;
            LaunchCount++;
            launchedThisStep = true;
            IsFlightActive = true;
            launchCooldownRemaining = settings.LaunchCooldown;
            spinChargeTime = 0f;
            launchGraceRemaining = 0f;
        }

        private void UpdateHover(bool validHeldMotion)
        {
            if (!validHeldMotion || movementReceiver.IsGrounded)
            {
                IsHovering = false;
                return;
            }

            if (IsHovering)
            {
                IsHovering = SpinMagnitude >=
                    settings.HoverDeactivationSpinSpeed;
            }
            else
            {
                IsHovering = IsSpinCharged &&
                    SpinMagnitude >= settings.HoverActivationSpinSpeed;
            }
        }

        private void UpdateMovementSupport(float deltaTime)
        {
            float targetGravityScale = IsHovering
                ? settings.HoverGravityMultiplier
                : 1f;
            currentGravityScale = Mathf.MoveTowards(
                currentGravityScale,
                targetGravityScale,
                settings.GravityRestorationRate * deltaTime);

            float lift = 0f;
            if (IsHovering && settings.HoverActivationSpinSpeed > 0f)
            {
                float spinRatio = Mathf.Clamp(
                    SpinMagnitude / settings.HoverActivationSpinSpeed,
                    .75f,
                    1.35f);
                lift = settings.HoverLiftAcceleration * spinRatio;
            }

            float steering = IsFlightActive || IsHovering
                ? settings.AirSteeringMultiplier
                : 1f;
            movementReceiver.SetFlightModifiers(
                currentGravityScale,
                lift,
                IsHovering ? settings.HoverDownwardDamping : 0f,
                settings.MaximumClimbSpeed,
                steering,
                settings.FlightDrag,
                settings.MaximumHorizontalSpeed);
        }

        private void StopFlightSupport(float deltaTime)
        {
            SpinMagnitude = 0f;
            spinChargeTime = 0f;
            launchGraceRemaining = 0f;
            launchMotionLatched = false;
            IsHovering = false;
            currentGravityScale = settings != null
                ? Mathf.MoveTowards(
                    currentGravityScale,
                    1f,
                    settings.GravityRestorationRate * deltaTime)
                : 1f;
            movementReceiver?.SetFlightModifiers(
                currentGravityScale,
                0f,
                0f,
                settings != null ? settings.MaximumClimbSpeed : 0f,
                1f,
                settings != null ? settings.FlightDrag : 0f,
                settings != null ? settings.MaximumHorizontalSpeed : 0f);
        }

        private void OnOwnerDied()
        {
            ResetFlight(true);
        }

        private void OnOwnerRespawned()
        {
            ResetFlight(true);
        }
    }
}
