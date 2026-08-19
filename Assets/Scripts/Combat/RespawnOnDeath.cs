using System;
using UnityEngine;

namespace HeroVR.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable))]
    public class RespawnOnDeath : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private float respawnDelay = 2f;

        private Damageable damageable;
        private CharacterController characterController;
        private Rigidbody body;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public bool IsRespawning { get; private set; }
        public event Action Respawned;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
            characterController = GetComponent<CharacterController>();
            body = GetComponent<Rigidbody>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void OnEnable()
        {
            damageable.Died += Respawn;
        }

        private void OnDisable()
        {
            damageable.Died -= Respawn;
            CancelInvoke(nameof(RespawnNow));
            IsRespawning = false;
        }

        public void SetRespawnDelay(float delay)
        {
            respawnDelay = Mathf.Max(0f, delay);
        }

        public void Respawn()
        {
            if (IsRespawning || !isActiveAndEnabled)
                return;

            IsRespawning = true;
            if (respawnDelay <= 0f)
                RespawnNow();
            else
                Invoke(nameof(RespawnNow), respawnDelay);
        }

        public void RespawnNow()
        {
            CancelInvoke(nameof(RespawnNow));

            Vector3 position = respawnPoint != null
                ? respawnPoint.position
                : initialPosition;
            Quaternion rotation = respawnPoint != null
                ? respawnPoint.rotation
                : initialRotation;

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            bool controllerWasEnabled = characterController != null && characterController.enabled;
            if (controllerWasEnabled)
                characterController.enabled = false;

            transform.SetPositionAndRotation(position, rotation);

            if (controllerWasEnabled)
                characterController.enabled = true;

            damageable.ResetHealth();
            IsRespawning = false;
            Respawned?.Invoke();
        }

        private void OnValidate()
        {
            respawnDelay = Mathf.Max(0f, respawnDelay);
        }
    }
}
