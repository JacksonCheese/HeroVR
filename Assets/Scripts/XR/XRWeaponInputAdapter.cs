using HeroVR.Combat;
using HeroVR.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.XR
{
    [DefaultExecutionOrder(-90)]
    [DisallowMultipleComponent]
    public sealed class XRWeaponInputAdapter : MonoBehaviour,
        IWeaponHoldStateSource
    {
        [SerializeField] private InputActionProperty gripAction;
        [SerializeField] private InputActionProperty recallAction;
        [SerializeField] private RecallableWeapon weapon;
        [SerializeField] private MonoBehaviour throwVelocityProviderSource;

        private IHitVelocityProvider throwVelocityProvider;
        private bool wasGripPressed;
        private bool throwArmed;

        public RecallableWeapon Weapon => weapon;
        public InputAction GripInputAction => gripAction.action;
        public InputAction RecallInputAction => recallAction.action;
        public bool IsWeaponHeld => wasGripPressed &&
            weapon != null &&
            weapon.State == RecallableWeaponState.Held;

        private void Awake()
        {
            throwVelocityProvider = throwVelocityProviderSource as IHitVelocityProvider;
        }

        private void OnEnable()
        {
            SetActionEnabled(gripAction, true);
            SetActionEnabled(recallAction, true);
            wasGripPressed = IsPressed(gripAction);
            throwArmed = wasGripPressed &&
                weapon != null &&
                weapon.State == RecallableWeaponState.Held;
        }

        private void OnDisable()
        {
            SetActionEnabled(gripAction, false);
            SetActionEnabled(recallAction, false);
            wasGripPressed = false;
            throwArmed = false;
        }

        public void Configure(
            InputActionProperty grip,
            InputActionProperty recall,
            RecallableWeapon recallableWeapon,
            MonoBehaviour velocityProvider)
        {
            gripAction = grip;
            recallAction = recall;
            weapon = recallableWeapon;
            throwVelocityProviderSource = velocityProvider;
            throwVelocityProvider = velocityProvider as IHitVelocityProvider;
        }

        private void Update()
        {
            bool gripPressed = IsPressed(gripAction);
            if (gripPressed && !wasGripPressed &&
                weapon != null &&
                weapon.State == RecallableWeaponState.Held)
            {
                throwArmed = true;
            }

            if (!gripPressed && wasGripPressed && throwArmed && weapon != null)
            {
                Vector3 velocity = throwVelocityProvider != null
                    ? throwVelocityProvider.Velocity
                    : Vector3.zero;
                weapon.TryThrow(velocity);
                throwArmed = false;
            }

            if (WasPressedThisFrame(recallAction) && weapon != null)
                weapon.BeginRecall();

            wasGripPressed = gripPressed;
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
    }
}
