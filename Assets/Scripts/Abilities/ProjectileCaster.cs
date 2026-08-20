using UnityEngine;

namespace HeroVR.Abilities
{
    public sealed class ProjectileCaster : HeroAbility
    {
        [SerializeField] private EnergyProjectile projectilePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private MonoBehaviour aimProviderSource;
        [SerializeField] private float projectileSpeed = 20f;
        [SerializeField, Min(0f)] private float projectileDamage = 25f;
        [SerializeField, Min(.01f)] private float projectileLifetime = 5f;
        [SerializeField, Min(0f)] private float projectileKnockbackImpulse = 6f;

        public EnergyProjectile ProjectilePrefab => projectilePrefab;
        public Transform SpawnPoint => spawnPoint;
        public IAimProvider AimProvider => aimProviderSource as IAimProvider;
        public EnergyProjectile LastSpawnedProjectile { get; private set; }

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

        public void SetAimProvider(MonoBehaviour provider)
        {
            aimProviderSource = provider is IAimProvider ? provider : null;
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
            IAimProvider aimProvider = AimProvider;
            Vector3 origin = aimProvider != null
                ? aimProvider.Origin
                : spawnPoint.position;
            Vector3 direction = aimProvider != null
                ? aimProvider.Direction
                : spawnPoint.forward;
            if (direction.sqrMagnitude <= .0001f)
                return false;

            direction.Normalize();
            Vector3 up = aimProvider != null ? aimProvider.Up : spawnPoint.up;
            if (Mathf.Abs(Vector3.Dot(direction, up.normalized)) > .999f)
                up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > .999f
                    ? Vector3.right
                    : Vector3.up;

            EnergyProjectile projectile = Instantiate(
                projectilePrefab,
                origin,
                Quaternion.LookRotation(direction, up));
            LastSpawnedProjectile = projectile;

            if (!projectile.gameObject.activeSelf)
                projectile.gameObject.SetActive(true);

            projectile.ConfigureCombat(
                projectileDamage,
                projectileLifetime,
                projectileKnockbackImpulse);
            projectile.Launch(
                direction * projectileSpeed,
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
