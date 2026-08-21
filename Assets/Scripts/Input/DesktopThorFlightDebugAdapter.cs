using HeroVR.Abilities;
using HeroVR.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.Input
{
    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    public sealed class DesktopThorFlightDebugAdapter : MonoBehaviour,
        IWeaponMotionSource,
        IWeaponHoldStateSource
    {
        [SerializeField] private InputActionProperty spinAction;
        [SerializeField] private InputActionProperty launchAction;
        [SerializeField] private RecallableWeapon weapon;
        [SerializeField] private MonoBehaviour aimProviderSource;
        [SerializeField, Min(0f)] private float simulatedSpinSpeed = 16f;
        [SerializeField, Min(0f)] private float simulatedLaunchSpeed = 8f;
        [SerializeField, Min(.01f)] private float launchPulseDuration = .08f;

        private IAimProvider aimProvider;
        private float launchPulseRemaining;

        public WeaponMotionSample CurrentMotion { get; private set; }
        public bool IsWeaponHeld => weapon != null &&
            weapon.State == RecallableWeaponState.Held;
        public InputAction SpinInputAction => spinAction.action;
        public InputAction LaunchInputAction => launchAction.action;

        private void Awake()
        {
            aimProvider = aimProviderSource as IAimProvider;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SetActionEnabled(spinAction, true);
            SetActionEnabled(launchAction, true);
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SetActionEnabled(spinAction, false);
            SetActionEnabled(launchAction, false);
#endif
            launchPulseRemaining = 0f;
            CurrentMotion = default;
        }

        public void Configure(
            InputActionProperty spinInput,
            InputActionProperty launchInput,
            RecallableWeapon recallableWeapon,
            MonoBehaviour aimSource,
            float spinSpeed,
            float launchSpeed)
        {
            spinAction = spinInput;
            launchAction = launchInput;
            weapon = recallableWeapon;
            aimProviderSource = aimSource;
            aimProvider = aimSource as IAimProvider;
            simulatedSpinSpeed = Mathf.Max(0f, spinSpeed);
            simulatedLaunchSpeed = Mathf.Max(0f, launchSpeed);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (WasPressedThisFrame(launchAction))
                launchPulseRemaining = launchPulseDuration;

            bool spinning = IsPressed(spinAction);
            Vector3 direction = aimProvider != null
                ? aimProvider.Direction
                : transform.forward;
            if (direction.sqrMagnitude <= .0001f)
                direction = transform.forward;

            Vector3 linearVelocity = launchPulseRemaining > 0f
                ? direction.normalized * simulatedLaunchSpeed
                : Vector3.zero;
            Vector3 angularVelocity = spinning
                ? Vector3.up * simulatedSpinSpeed
                : Vector3.zero;
            GameObject owner = weapon != null && weapon.Owner != null
                ? weapon.Owner.gameObject
                : null;
            CurrentMotion = new WeaponMotionSample(
                linearVelocity,
                angularVelocity,
                IsWeaponHeld,
                owner);
            launchPulseRemaining = Mathf.Max(
                0f,
                launchPulseRemaining - Time.deltaTime);
#else
            CurrentMotion = default;
#endif
        }

        private static bool IsPressed(InputActionProperty actionProperty)
        {
            InputAction action = actionProperty.action;
            return action != null && action.IsPressed();
        }

        private static bool WasPressedThisFrame(InputActionProperty actionProperty)
        {
            InputAction action = actionProperty.action;
            return action != null && action.WasPressedThisFrame();
        }

        private static void SetActionEnabled(
            InputActionProperty actionProperty,
            bool enabled)
        {
            InputAction action = actionProperty.action;
            if (action == null)
                return;

            if (enabled)
            {
                action.Enable();
            }
            else if (actionProperty.reference == null)
            {
                action.Disable();
            }
        }

        private void OnValidate()
        {
            simulatedSpinSpeed = Mathf.Max(0f, simulatedSpinSpeed);
            simulatedLaunchSpeed = Mathf.Max(0f, simulatedLaunchSpeed);
            launchPulseDuration = Mathf.Max(.01f, launchPulseDuration);
        }
    }
}
