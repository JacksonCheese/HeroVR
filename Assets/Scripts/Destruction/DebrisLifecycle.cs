using System.Collections.Generic;
using UnityEngine;

namespace HeroVR.Destruction
{
    [DisallowMultipleComponent]
    public sealed class DebrisLifecycle : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float activeLifetime = 6f;
        [SerializeField, Min(1)] private int maximumActiveDebris = 24;

        private static readonly List<DebrisLifecycle> ActiveDebris =
            new List<DebrisLifecycle>();

        private Rigidbody[] bodies;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private float deactivateTime;
        private bool active;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (active && activeLifetime > 0f && Time.time >= deactivateTime)
                Deactivate();
        }

        public void Configure(float lifetime, int activeLimit)
        {
            activeLifetime = Mathf.Max(0f, lifetime);
            maximumActiveDebris = Mathf.Max(1, activeLimit);
        }

        public void Activate()
        {
            EnsureInitialized();
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            active = true;
            deactivateTime = Time.time + activeLifetime;
            ActiveDebris.Remove(this);
            ActiveDebris.Add(this);

            for (int index = 0; index < bodies.Length; index++)
            {
                bodies[index].isKinematic = false;
                bodies[index].WakeUp();
            }

            while (ActiveDebris.Count > maximumActiveDebris)
            {
                DebrisLifecycle oldest = ActiveDebris[0];
                ActiveDebris.RemoveAt(0);
                if (oldest != null && oldest != this)
                    oldest.Deactivate();
            }
        }

        public void ResetDebris()
        {
            EnsureInitialized();
            ActiveDebris.Remove(this);
            active = false;
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            for (int index = 0; index < bodies.Length; index++)
            {
                bodies[index].linearVelocity = Vector3.zero;
                bodies[index].angularVelocity = Vector3.zero;
                bodies[index].isKinematic = true;
            }
            gameObject.SetActive(false);
        }

        private void Deactivate()
        {
            EnsureInitialized();
            ActiveDebris.Remove(this);
            active = false;
            for (int index = 0; index < bodies.Length; index++)
            {
                bodies[index].linearVelocity = Vector3.zero;
                bodies[index].angularVelocity = Vector3.zero;
                bodies[index].isKinematic = true;
            }
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            ActiveDebris.Remove(this);
        }

        private void EnsureInitialized()
        {
            if (bodies != null)
                return;

            bodies = GetComponentsInChildren<Rigidbody>(true);
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
        }

        private void OnValidate()
        {
            activeLifetime = Mathf.Max(0f, activeLifetime);
            maximumActiveDebris = Mathf.Max(1, maximumActiveDebris);
        }
    }
}
