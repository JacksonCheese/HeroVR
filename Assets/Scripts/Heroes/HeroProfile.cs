using HeroVR.Abilities;
using HeroVR.Combat;
using HeroVR.Movement;
using HeroVR.Weapons;
using HeroVR.XR;
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

            ConfigureLocomotion();
            ConfigureMelee();
            ConfigureProjectile();
            ConfigureLightning();
            ConfigureDash();
            ConfigureUltimate();
            ConfigurePhysicalPunches();
            ConfigureWeapons();
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

        private void ConfigureLocomotion()
        {
            if (!definition.OverrideLocomotion)
                return;

            HeroDefinition.LocomotionSettings settings = definition.Locomotion;
            DesktopCharacterMotor desktopMotor = GetComponent<DesktopCharacterMotor>();
            if (desktopMotor != null)
                desktopMotor.ConfigureMovement(settings.moveSpeed, settings.jumpHeight);

            XRCharacterMotor xrMotor = GetComponent<XRCharacterMotor>();
            if (xrMotor != null)
                xrMotor.ConfigureMovement(settings.moveSpeed, settings.jumpHeight);
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
            dash.SetDuration(settings.duration > 0f ? settings.duration : .22f);
        }

        private void ConfigureLightning()
        {
            ConfigureLightningAbility(loadout.PrimaryAttack as LightningAbility);
            if (loadout.SecondaryAttack != loadout.PrimaryAttack)
                ConfigureLightningAbility(loadout.SecondaryAttack as LightningAbility);
        }

        private void ConfigureLightningAbility(LightningAbility lightning)
        {
            if (lightning == null)
                return;

            HeroDefinition.LightningSettings settings = definition.Lightning;
            lightning.SetCooldown(settings.cooldown);
            lightning.ConfigureCombat(
                settings.range,
                settings.damage,
                settings.knockbackImpulse,
                settings.visualDuration);
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

        private void ConfigureWeapons()
        {
            HeroDefinition.WeaponSettings settings = definition.Weapon;
            RecallableWeapon[] weapons =
                GetComponentsInChildren<RecallableWeapon>(true);
            for (int index = 0; index < weapons.Length; index++)
            {
                weapons[index].ConfigureOwner(health);
                weapons[index].ConfigureMotion(
                    settings.throwVelocityMultiplier,
                    settings.maximumThrowSpeed,
                    settings.recallSpeed,
                    settings.recallAcceleration);
                weapons[index].ConfigureImpact(
                    settings.minimumHitSpeed,
                    settings.damagePerSpeed,
                    settings.maximumDamage,
                    settings.knockbackMultiplier,
                    settings.maximumKnockbackImpulse,
                    settings.contactCooldown);
            }
        }
    }
}
