using UnityEngine;

namespace HeroVR.Abilities
{
    public sealed class ProjectileCaster : HeroAbility
    {
        [SerializeField] private EnergyProjectile projectilePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float projectileSpeed = 20f;
        [SerializeField, Min(0f)] private float projectileDamage = 25f;
        [SerializeField, Min(.01f)] private float projectileLifetime = 5f;
        [SerializeField, Min(0f)] private float projectileKnockbackImpulse = 6f;

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

        public void ConfigureCombat(
            float damage,
            float lifetime,
            float knockbackImpulse)
        {
            projectileDamage = Mathf.Max(0f, damage);
            projectileLifetime = Mathf.Max(.01f, lifetime);
            projectileKnockbackImpulse = Mathf.Max(0f, knockbackImpulse);
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

            projectile.ConfigureCombat(
                projectileDamage,
                projectileLifetime,
                projectileKnockbackImpulse);
            projectile.Launch(
                spawnPoint.forward * projectileSpeed,
                Owner);

            return true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileDamage = Mathf.Max(0f, projectileDamage);
            projectileLifetime = Mathf.Max(.01f, projectileLifetime);
            projectileKnockbackImpulse = Mathf.Max(0f, projectileKnockbackImpulse);
        }
    }
}
