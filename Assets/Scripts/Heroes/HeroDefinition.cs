using System;
using UnityEngine;

namespace HeroVR.Heroes
{
    [CreateAssetMenu(
        fileName = "HeroDefinition",
        menuName = "HeroVR/Hero Definition")]
    public sealed class HeroDefinition : ScriptableObject
    {
        [Serializable]
        public struct MeleeSettings
        {
            [Min(0f)] public float cooldown;
            [Min(0f)] public float damage;
            [Min(0f)] public float range;
            [Min(0f)] public float radius;
            [Min(0f)] public float knockbackImpulse;
        }

        [Serializable]
        public struct PhysicalPunchSettings
        {
            [Min(0f)] public float minimumSpeed;
            [Min(0f)] public float damagePerSpeed;
            [Min(0f)] public float maximumDamage;
            [Min(0f)] public float knockbackMultiplier;
            [Min(0f)] public float maximumKnockbackImpulse;
        }

        [Serializable]
        public struct ProjectileSettings
        {
            [Min(0f)] public float cooldown;
            [Min(0f)] public float damage;
            [Min(0f)] public float speed;
            [Min(.01f)] public float lifetime;
            [Min(0f)] public float knockbackImpulse;
        }

        [Serializable]
        public struct DashSettings
        {
            [Min(0f)] public float cooldown;
            [Min(0f)] public float distance;
        }

        [Serializable]
        public struct UltimateSettings
        {
            [Min(0f)] public float cooldown;
            [Min(0f)] public float radius;
            [Min(0f)] public float damage;
            [Min(0f)] public float knockbackImpulse;
        }

        [Header("Identity")]
        [SerializeField] private string heroId = "kinetic-vanguard";
        [SerializeField] private string displayName = "Kinetic Vanguard";
        [SerializeField, TextArea] private string description =
            "A close-range kinetic fighter who converts combat momentum into a devastating nova.";
        [SerializeField] private Color signatureColor =
            new Color(.1f, .65f, 1f);

        [Header("Ability Names")]
        [SerializeField] private string resourceName = "Momentum";
        [SerializeField] private string primaryName = "Kinetic Punch";
        [SerializeField] private string secondaryName = "Kinetic Bolt";
        [SerializeField] private string movementName = "Burst Dash";
        [SerializeField] private string ultimateName = "Kinetic Nova";

        [Header("Survivability")]
        [SerializeField, Min(.01f)] private float maxHealth = 125f;

        [Header("Momentum")]
        [SerializeField, Min(.01f)] private float maximumUltimateCharge = 100f;
        [SerializeField, Min(0f)] private float chargePerDamageDealt = 1f;
        [SerializeField, Min(0f)] private float chargePerDamageTaken = .5f;

        [Header("Primary — Kinetic Punch")]
        [SerializeField] private MeleeSettings melee = new MeleeSettings
        {
            cooldown = .32f,
            damage = 24f,
            range = 1.6f,
            radius = .7f,
            knockbackImpulse = 10f
        };

        [Header("XR Physical Punch")]
        [SerializeField] private PhysicalPunchSettings physicalPunch =
            new PhysicalPunchSettings
            {
                minimumSpeed = 1.4f,
                damagePerSpeed = 8f,
                maximumDamage = 32f,
                knockbackMultiplier = 2f,
                maximumKnockbackImpulse = 14f
            };

        [Header("Secondary — Kinetic Bolt")]
        [SerializeField] private ProjectileSettings projectile =
            new ProjectileSettings
            {
                cooldown = .45f,
                damage = 24f,
                speed = 26f,
                lifetime = 5f,
                knockbackImpulse = 7f
            };

        [Header("Movement — Burst Dash")]
        [SerializeField] private DashSettings dash = new DashSettings
        {
            cooldown = 1.2f,
            distance = 6f
        };

        [Header("Ultimate — Kinetic Nova")]
        [SerializeField] private UltimateSettings ultimate =
            new UltimateSettings
            {
                cooldown = 1f,
                radius = 4.5f,
                damage = 38f,
                knockbackImpulse = 18f
            };

        public string HeroId => heroId;
        public string DisplayName => displayName;
        public string Description => description;
        public Color SignatureColor => signatureColor;
        public string ResourceName => resourceName;
        public string PrimaryName => primaryName;
        public string SecondaryName => secondaryName;
        public string MovementName => movementName;
        public string UltimateName => ultimateName;
        public float MaxHealth => maxHealth;
        public float MaximumUltimateCharge => maximumUltimateCharge;
        public float ChargePerDamageDealt => chargePerDamageDealt;
        public float ChargePerDamageTaken => chargePerDamageTaken;
        public MeleeSettings Melee => melee;
        public PhysicalPunchSettings PhysicalPunch => physicalPunch;
        public ProjectileSettings Projectile => projectile;
        public DashSettings Dash => dash;
        public UltimateSettings Ultimate => ultimate;

        public void ConfigureIdentity(
            string id,
            string heroName,
            string heroDescription,
            Color color)
        {
            heroId = string.IsNullOrWhiteSpace(id) ? "hero" : id.Trim();
            displayName = string.IsNullOrWhiteSpace(heroName)
                ? "Unnamed Hero"
                : heroName.Trim();
            description = heroDescription ?? string.Empty;
            signatureColor = color;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(.01f, maxHealth);
            maximumUltimateCharge = Mathf.Max(.01f, maximumUltimateCharge);
            chargePerDamageDealt = Mathf.Max(0f, chargePerDamageDealt);
            chargePerDamageTaken = Mathf.Max(0f, chargePerDamageTaken);
        }
    }
}
