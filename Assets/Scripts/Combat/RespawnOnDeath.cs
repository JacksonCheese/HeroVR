using UnityEngine;

namespace HeroVR.Combat
{
    [RequireComponent(typeof(Damageable))]
    public class RespawnOnDeath : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private float respawnDelay = 2f;

        private Damageable damageable;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
        }

        public void Respawn()
        {
            Invoke(nameof(DoRespawn), respawnDelay);
        }

        private void DoRespawn()
        {
            if (respawnPoint != null)
            {
                transform.SetPositionAndRotation(
                    respawnPoint.position,
                    respawnPoint.rotation
                );
            }

            damageable.ResetHealth();
        }
    }
}
