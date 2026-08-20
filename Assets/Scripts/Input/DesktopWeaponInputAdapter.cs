using HeroVR.Abilities;
using HeroVR.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.Input
{
    [DisallowMultipleComponent]
    public sealed class DesktopWeaponInputAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionProperty throwAction;
        [SerializeField] private InputActionProperty recallAction;
        [SerializeField] private RecallableWeapon weapon;
        [SerializeField] private MonoBehaviour aimProviderSource;
        [SerializeField, Min(0f)] private float throwInputSpeed = 18f;

        private IAimProvider aimProvider;

        public RecallableWeapon Weapon => weapon;
        public InputAction ThrowInputAction => throwAction.action;
        public InputAction RecallInputAction => recallAction.action;

        private void Awake()
        {
            aimProvider = aimProviderSource as IAimProvider;
        }

        private void OnEnable()
        {
            SetActionEnabled(throwAction, true);
            SetActionEnabled(recallAction, true);
        }

        private void OnDisable()
        {
            SetActionEnabled(throwAction, false);
            SetActionEnabled(recallAction, false);
        }

        public void Configure(
            InputActionProperty throwInput,
            InputActionProperty recallInput,
            RecallableWeapon recallableWeapon,
            MonoBehaviour aimSource,
            float inputSpeed)
        {
            throwAction = throwInput;
            recallAction = recallInput;
            weapon = recallableWeapon;
            aimProviderSource = aimSource;
            aimProvider = aimSource as IAimProvider;
            throwInputSpeed = Mathf.Max(0f, inputSpeed);
        }

        public bool TryThrow()
        {
            if (weapon == null)
                return false;

            Vector3 direction = aimProvider != null
                ? aimProvider.Direction
                : transform.forward;
            if (direction.sqrMagnitude <= .0001f)
                return false;

            return weapon.TryThrow(direction.normalized * throwInputSpeed);
        }

        public bool TryRecall()
        {
            return weapon != null && weapon.BeginRecall();
        }

        private void Update()
        {
            if (WasPressedThisFrame(throwAction))
                TryThrow();

            if (WasPressedThisFrame(recallAction))
                TryRecall();
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
            throwInputSpeed = Mathf.Max(0f, throwInputSpeed);
        }
    }
}
