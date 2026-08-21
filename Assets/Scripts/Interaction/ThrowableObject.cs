using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ThrowableObject : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float throwVelocityMultiplier = 1f;
        [SerializeField, Min(0f)] private float maximumThrowSpeed = 26f;

        private Rigidbody body;
        private ImpactDamageDealer impactDamageDealer;

        public GameObject LastInstigator { get; private set; }
        public Vector3 LastThrowVelocity { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            impactDamageDealer = GetComponent<ImpactDamageDealer>();
        }

        public void Configure(float velocityMultiplier, float maxSpeed)
        {
            throwVelocityMultiplier = Mathf.Max(0f, velocityMultiplier);
            maximumThrowSpeed = Mathf.Max(0f, maxSpeed);
        }

        public Vector3 Throw(GameObject instigator, Vector3 inheritedVelocity)
        {
            LastInstigator = instigator;
            LastThrowVelocity = Vector3.ClampMagnitude(
                inheritedVelocity * throwVelocityMultiplier,
                maximumThrowSpeed);
            body.isKinematic = false;
            body.WakeUp();
            body.linearVelocity = LastThrowVelocity;

            if (impactDamageDealer == null)
                impactDamageDealer = GetComponent<ImpactDamageDealer>();
            if (impactDamageDealer != null)
                impactDamageDealer.SetInstigator(instigator);

            return LastThrowVelocity;
        }

        private void OnValidate()
        {
            throwVelocityMultiplier = Mathf.Max(0f, throwVelocityMultiplier);
            maximumThrowSpeed = Mathf.Max(0f, maximumThrowSpeed);
        }
    }
}
