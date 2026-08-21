using System;
using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Destruction
{
    [DisallowMultipleComponent]
    public sealed class StructuralDamageReceiver : MonoBehaviour,
        ICombatDamageReceiver
    {
        [Header("Durability")]
        [SerializeField, Min(.01f)] private float maximumStructuralHealth = 100f;
        [SerializeField, Min(0f)] private float armor = 5f;
        [SerializeField, Min(0f)] private float minimumImpactStrength = 10f;
        [SerializeField, Min(0f)] private float impactDamageScale = .4f;

        [Header("Damage resistance")]
        [SerializeField, Min(0f)] private float physicalMultiplier = .12f;
        [SerializeField, Min(0f)] private float heavyPhysicalMultiplier = 1f;
        [SerializeField, Min(0f)] private float energyMultiplier = .3f;
        [SerializeField, Min(0f)] private float structuralMultiplier = 1.25f;

        public float CurrentStructuralHealth { get; private set; }
        public float MaximumStructuralHealth => maximumStructuralHealth;
        public float HealthFraction => maximumStructuralHealth > 0f
            ? CurrentStructuralHealth / maximumStructuralHealth
            : 0f;
        public bool IsBroken => CurrentStructuralHealth <= 0f;
        public DamageInfo LastDamageInfo { get; private set; }
        public float LastAppliedDamage { get; private set; }

        public event Action<DamageInfo, float> StructuralImpactReceived;
        public event Action<float, float> HealthChanged;
        public event Action Broken;

        private void Awake()
        {
            CurrentStructuralHealth = maximumStructuralHealth;
        }

        public void Configure(
            float maximumHealth,
            float flatArmor,
            float minimumImpact,
            float impactScaling,
            float physicalResistanceMultiplier,
            float heavyMultiplier,
            float energyResistanceMultiplier,
            float directStructuralMultiplier)
        {
            maximumStructuralHealth = Mathf.Max(.01f, maximumHealth);
            armor = Mathf.Max(0f, flatArmor);
            minimumImpactStrength = Mathf.Max(0f, minimumImpact);
            impactDamageScale = Mathf.Max(0f, impactScaling);
            physicalMultiplier = Mathf.Max(0f, physicalResistanceMultiplier);
            heavyPhysicalMultiplier = Mathf.Max(0f, heavyMultiplier);
            energyMultiplier = Mathf.Max(0f, energyResistanceMultiplier);
            structuralMultiplier = Mathf.Max(0f, directStructuralMultiplier);
            CurrentStructuralHealth = maximumStructuralHealth;
        }

        public bool TryReceiveDamage(DamageInfo damageInfo)
        {
            if (IsBroken || damageInfo.Amount <= 0f)
                return false;

            float typeMultiplier = GetTypeMultiplier(damageInfo.DamageType);
            bool isHeavyType = damageInfo.DamageType == DamageType.HeavyPhysical ||
                damageInfo.DamageType == DamageType.Structural;
            if (!isHeavyType &&
                damageInfo.ImpactStrength < minimumImpactStrength &&
                damageInfo.DamageType != DamageType.Energy)
            {
                return false;
            }

            float typedDamage = damageInfo.Amount * typeMultiplier;
            float impactDamage = Mathf.Max(
                0f,
                damageInfo.ImpactStrength - minimumImpactStrength) *
                impactDamageScale;
            float appliedDamage = Mathf.Max(0f, typedDamage + impactDamage - armor);
            if (appliedDamage <= 0f)
                return false;

            bool wasBroken = IsBroken;
            CurrentStructuralHealth = Mathf.Max(
                0f,
                CurrentStructuralHealth - appliedDamage);
            LastDamageInfo = damageInfo;
            LastAppliedDamage = appliedDamage;
            StructuralImpactReceived?.Invoke(damageInfo, appliedDamage);
            HealthChanged?.Invoke(CurrentStructuralHealth, maximumStructuralHealth);
            if (!wasBroken && IsBroken)
                Broken?.Invoke();
            return true;
        }

        public void ResetStructure()
        {
            CurrentStructuralHealth = maximumStructuralHealth;
            LastDamageInfo = default;
            LastAppliedDamage = 0f;
            HealthChanged?.Invoke(CurrentStructuralHealth, maximumStructuralHealth);
        }

        private float GetTypeMultiplier(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.HeavyPhysical:
                    return heavyPhysicalMultiplier;
                case DamageType.Energy:
                    return energyMultiplier;
                case DamageType.Structural:
                    return structuralMultiplier;
                default:
                    return physicalMultiplier;
            }
        }

        private void OnValidate()
        {
            maximumStructuralHealth = Mathf.Max(.01f, maximumStructuralHealth);
            armor = Mathf.Max(0f, armor);
            minimumImpactStrength = Mathf.Max(0f, minimumImpactStrength);
            impactDamageScale = Mathf.Max(0f, impactDamageScale);
            physicalMultiplier = Mathf.Max(0f, physicalMultiplier);
            heavyPhysicalMultiplier = Mathf.Max(0f, heavyPhysicalMultiplier);
            energyMultiplier = Mathf.Max(0f, energyMultiplier);
            structuralMultiplier = Mathf.Max(0f, structuralMultiplier);
        }
    }
}
