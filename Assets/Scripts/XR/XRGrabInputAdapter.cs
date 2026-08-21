using HeroVR.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.XR
{
    [DefaultExecutionOrder(-85)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhysicsGrabInteractor))]
    public sealed class XRGrabInputAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionProperty gripAction;

        private PhysicsGrabInteractor interactor;

        public InputAction GripInputAction => gripAction.action;

        private void Awake()
        {
            interactor = GetComponent<PhysicsGrabInteractor>();
        }

        private void OnEnable()
        {
            if (gripAction.action != null)
                gripAction.action.Enable();
        }

        private void OnDisable()
        {
            if (gripAction.reference == null && gripAction.action != null)
                gripAction.action.Disable();
            if (interactor != null && interactor.IsHolding)
                interactor.Release(Vector3.zero);
        }

        public void Configure(InputActionProperty grip)
        {
            gripAction = grip;
        }

        private void Update()
        {
            InputAction action = gripAction.action;
            if (action == null)
                return;

            if (action.WasPressedThisFrame())
                interactor.TryBeginGrab();
            if (action.WasReleasedThisFrame())
                interactor.Release();
        }
    }
}
