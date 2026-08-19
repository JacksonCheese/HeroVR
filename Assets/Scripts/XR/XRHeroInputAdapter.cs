using HeroVR.Abilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.XR
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRCharacterMotor), typeof(HeroAbilityLoadout))]
    public sealed class XRHeroInputAdapter : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField] private InputActionProperty moveAction;
        [SerializeField] private InputActionProperty turnAction;
        [SerializeField] private InputActionProperty jumpAction;

        [Header("Abilities")]
        [SerializeField] private InputActionProperty primaryAction;
        [SerializeField] private InputActionProperty secondaryAction;
        [SerializeField] private InputActionProperty dashAction;
        [SerializeField] private InputActionProperty ultimateAction;

        [Header("Comfort")]
        [SerializeField, Range(.1f, 1f)] private float turnActivationThreshold = .75f;
        [SerializeField, Range(0f, .9f)] private float turnResetThreshold = .25f;

        private XRCharacterMotor motor;
        private HeroAbilityLoadout loadout;
        private bool turnLatched;

        private void Awake()
        {
            motor = GetComponent<XRCharacterMotor>();
            loadout = GetComponent<HeroAbilityLoadout>();
        }

        private void OnEnable()
        {
            SetActionsEnabled(true);
        }

        private void OnDisable()
        {
            SetActionsEnabled(false);
            turnLatched = false;

            if (motor != null)
                motor.SetMoveInput(Vector2.zero);
        }

        public void Configure(
            InputActionProperty move,
            InputActionProperty turn,
            InputActionProperty jump,
            InputActionProperty primary,
            InputActionProperty secondary,
            InputActionProperty dash,
            InputActionProperty ultimate)
        {
            moveAction = move;
            turnAction = turn;
            jumpAction = jump;
            primaryAction = primary;
            secondaryAction = secondary;
            dashAction = dash;
            ultimateAction = ultimate;
        }

        private void Update()
        {
            motor.SetMoveInput(ReadValue<Vector2>(moveAction));
            UpdateSnapTurn(ReadValue<Vector2>(turnAction).x);

            if (WasPressedThisFrame(jumpAction))
                motor.RequestJump();

            if (WasPressedThisFrame(primaryAction))
                loadout.TryActivatePrimary();

            if (WasPressedThisFrame(secondaryAction))
                loadout.TryActivateSecondary();

            if (WasPressedThisFrame(dashAction))
                loadout.TryActivateMovementAbility(motor.DesiredWorldMoveDirection);

            if (WasPressedThisFrame(ultimateAction))
                loadout.TryActivateUltimate();
        }

        private void UpdateSnapTurn(float turnInput)
        {
            float magnitude = Mathf.Abs(turnInput);
            if (!turnLatched && magnitude >= turnActivationThreshold)
            {
                motor.RequestSnapTurn(turnInput);
                turnLatched = true;
            }
            else if (turnLatched && magnitude <= turnResetThreshold)
            {
                turnLatched = false;
            }
        }

        private void SetActionsEnabled(bool enabled)
        {
            SetActionEnabled(moveAction, enabled);
            SetActionEnabled(turnAction, enabled);
            SetActionEnabled(jumpAction, enabled);
            SetActionEnabled(primaryAction, enabled);
            SetActionEnabled(secondaryAction, enabled);
            SetActionEnabled(dashAction, enabled);
            SetActionEnabled(ultimateAction, enabled);
        }

        private static void SetActionEnabled(
            InputActionProperty actionProperty,
            bool enabled)
        {
            InputAction action = actionProperty.action;
            if (action == null)
                return;

            if (enabled)
                action.Enable();
            else
                action.Disable();
        }

        private static TValue ReadValue<TValue>(InputActionProperty actionProperty)
            where TValue : struct
        {
            InputAction action = actionProperty.action;
            return action != null ? action.ReadValue<TValue>() : default;
        }

        private static bool WasPressedThisFrame(InputActionProperty actionProperty)
        {
            InputAction action = actionProperty.action;
            return action != null && action.WasPressedThisFrame();
        }

        private void OnValidate()
        {
            turnResetThreshold = Mathf.Min(
                turnResetThreshold,
                turnActivationThreshold);
        }
    }
}
