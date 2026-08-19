using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.Abilities
{
    public class ProjectileCaster : MonoBehaviour
    {
        [SerializeField] private EnergyProjectile projectilePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private InputActionProperty fireAction;
        [SerializeField] private float projectileSpeed = 20f;
        [SerializeField] private float cooldown = 0.4f;

        private float nextFireTime;

        private void OnEnable()
        {
            fireAction.action.Enable();
            fireAction.action.performed += Fire;
        }

        private void OnDisable()
        {
            fireAction.action.performed -= Fire;
            fireAction.action.Disable();
        }

        private void Fire(InputAction.CallbackContext context)
        {
            if (Time.time < nextFireTime || projectilePrefab == null || spawnPoint == null)
                return;

            nextFireTime = Time.time + cooldown;

            EnergyProjectile projectile =
                Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);

            projectile.Launch(spawnPoint.forward * projectileSpeed);
        }
    }
}
