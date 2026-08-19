using UnityEngine;

namespace HeroVR.Abilities
{
    public sealed class ProjectileCaster : HeroAbility
    {
        [SerializeField] private EnergyProjectile projectilePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float projectileSpeed = 20f;

        public EnergyProjectile ProjectilePrefab => projectilePrefab;
        public Transform SpawnPoint => spawnPoint;

        public void Configure(
            EnergyProjectile prefab,
            Transform projectileSpawnPoint,
            float speed)
        {
            projectilePrefab = prefab;
            spawnPoint = projectileSpawnPoint;
            projectileSpeed = Mathf.Max(0f, speed);
        }

        public void SetProjectilePrefab(EnergyProjectile prefab)
        {
            projectilePrefab = prefab;
        }

        public void SetSpawnPoint(Transform projectileSpawnPoint)
        {
            spawnPoint = projectileSpawnPoint;
        }

        protected override bool CanActivate()
        {
            return projectilePrefab != null && spawnPoint != null;
        }

        protected override bool Activate()
        {
            EnergyProjectile projectile = Instantiate(
                projectilePrefab,
                spawnPoint.position,
                spawnPoint.rotation);

            if (!projectile.gameObject.activeSelf)
                projectile.gameObject.SetActive(true);

            projectile.Launch(
                spawnPoint.forward * projectileSpeed,
                Owner);

            return true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
        }
    }
}
