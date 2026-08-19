using HeroVR.Abilities;
using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Heroes
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(Damageable),
        typeof(HeroAbilityLoadout),
        typeof(HeroUltimateCharge))]
    public sealed class HeroProfile : MonoBehaviour
    {
        [SerializeField] private HeroDefinition definition;

        private Damageable health;
        private HeroAbilityLoadout loadout;
        private HeroUltimateCharge ultimateCharge;

        public HeroDefinition Definition => definition;

        private void Awake()
        {
            ApplyDefinition();
        }

        public void Configure(HeroDefinition heroDefinition)
        {
            definition = heroDefinition;
            ApplyDefinition();
        }

        public void ApplyDefinition()
        {
            if (definition == null)
                return;

            CacheComponents();
            health.SetMaxHealth(definition.MaxHealth);
            ultimateCharge.Configure(
                definition.MaximumUltimateCharge,
                definition.ChargePerDamageDealt,
                definition.ChargePerDamageTaken);

            ConfigureMelee();
            ConfigureProjectile();
            ConfigureDash();
            ConfigureUltimate();
            ConfigurePhysicalPunches();
        }

        private void CacheComponents()
        {
            health = health != null ? health : GetComponent<Damageable>();
            loadout = loadout != null ? loadout : GetComponent<HeroAbilityLoadout>();
            ultimateCharge = ultimateCharge != null
                ? ultimateCharge
                : GetComponent<HeroUltimateCharge>();
        }

        private void ConfigureMelee()
        {
            if (!(loadout.PrimaryAttack is MeleePunchAbility melee))
                return;

            HeroDefinition.MeleeSettings settings = definition.Melee;
            melee.SetCooldown(settings.cooldown);
            melee.ConfigureCombat(
                settings.damage,
                settings.range,
                settings.radius,
                settings.knockbackImpulse);
        }

        private void ConfigureProjectile()
        {
            if (!(loadout.SecondaryAttack is ProjectileCaster projectile))
                return;

            HeroDefinition.ProjectileSettings settings = definition.Projectile;
            projectile.SetCooldown(settings.cooldown);
            projectile.Configure(
                projectile.ProjectilePrefab,
                projectile.SpawnPoint,
                settings.speed);
            projectile.ConfigureCombat(
                settings.damage,
                settings.lifetime,
                settings.knockbackImpulse);
        }

        private void ConfigureDash()
        {
            if (!(loadout.MovementAbility is DashAbility dash))
                return;

            HeroDefinition.DashSettings settings = definition.Dash;
            dash.SetCooldown(settings.cooldown);
            dash.SetDistance(settings.distance);
        }

        private void ConfigureUltimate()
        {
            if (!(loadout.UltimateAbility is RadialSmashAbility ultimate))
                return;

            HeroDefinition.UltimateSettings settings = definition.Ultimate;
            ultimate.SetCooldown(settings.cooldown);
            ultimate.ConfigureCombat(
                settings.radius,
                settings.damage,
                settings.knockbackImpulse);
        }

        private void ConfigurePhysicalPunches()
        {
            HeroDefinition.PhysicalPunchSettings settings =
                definition.PhysicalPunch;
            PunchHitbox[] hitboxes = GetComponentsInChildren<PunchHitbox>(true);
            for (int index = 0; index < hitboxes.Length; index++)
            {
                hitboxes[index].Configure(
                    settings.minimumSpeed,
                    settings.damagePerSpeed,
                    settings.maximumDamage,
                    settings.knockbackMultiplier,
                    settings.maximumKnockbackImpulse);
            }
        }
    }
}
