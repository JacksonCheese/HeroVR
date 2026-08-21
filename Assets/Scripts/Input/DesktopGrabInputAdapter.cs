using HeroVR.Abilities;
using HeroVR.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.Input
{
    [DefaultExecutionOrder(-80)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhysicsGrabInteractor))]
    public sealed class DesktopGrabInputAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionProperty grabAction;
        [SerializeField] private MonoBehaviour aimProviderSource;
        [SerializeField, Min(0f)] private float grabReach = 2.25f;
        [SerializeField, Min(0f)] private float debugThrowSpeed = 14f;

        private PhysicsGrabInteractor interactor;

        public InputAction GrabInputAction => grabAction.action;
        public IAimProvider AimProvider => aimProviderSource as IAimProvider;

        private void Awake()
        {
            interactor = GetComponent<PhysicsGrabInteractor>();
        }

        private void OnEnable()
        {
            if (grabAction.action != null)
                grabAction.action.Enable();
        }

        private void OnDisable()
        {
            if (grabAction.reference == null && grabAction.action != null)
                grabAction.action.Disable();
            if (interactor != null && interactor.IsHolding)
                interactor.Release(Vector3.zero);
        }

        public void Configure(
            InputActionProperty grab,
            MonoBehaviour aimProvider,
            float reach,
            float throwSpeed)
        {
            grabAction = grab;
            aimProviderSource = aimProvider is IAimProvider ? aimProvider : null;
            grabReach = Mathf.Max(0f, reach);
            debugThrowSpeed = Mathf.Max(0f, throwSpeed);
        }

        private void Update()
        {
            InputAction action = grabAction.action;
            IAimProvider aim = AimProvider;
            if (action == null || aim == null)
                return;

            if (action.WasPressedThisFrame())
            {
                transform.position = aim.Origin + aim.Direction * grabReach;
                interactor.TryBeginGrab(transform.position);
            }

            if (action.WasReleasedThisFrame())
                interactor.Release(aim.Direction * debugThrowSpeed);
        }

        private void OnValidate()
        {
            grabReach = Mathf.Max(0f, grabReach);
            debugThrowSpeed = Mathf.Max(0f, debugThrowSpeed);
        }
    }
}
