using System;
using HeroVR.Abilities;
using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Heroes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable))]
    public sealed class HeroUltimateCharge : MonoBehaviour,
        IUltimateResource,
        IDamageDealtReceiver
    {
        [SerializeField, Min(.01f)] private float maximumCharge = 100f;
        [SerializeField, Min(0f)] private float chargePerDamageDealt = 1f;
        [SerializeField, Min(0f)] private float chargePerDamageTaken = .5f;

        private Damageable ownerHealth;
        private float previousHealth;

        public float CurrentCharge { get; private set; }
        public float MaximumCharge => maximumCharge;
        public float NormalizedCharge => maximumCharge <= 0f
            ? 0f
            : CurrentCharge / maximumCharge;
        public bool IsUltimateReady => CurrentCharge >= maximumCharge;

        public event Action<float, float> ChargeChanged;

        private void Awake()
        {
            ownerHealth = GetComponent<Damageable>();
            previousHealth = ownerHealth.CurrentHealth;
        }

        private void OnEnable()
        {
            ownerHealth.Damaged += OnOwnerDamaged;
            ownerHealth.HealthChanged += OnHealthChanged;
            ownerHealth.Died += ResetCharge;
            previousHealth = ownerHealth.CurrentHealth;
        }

        private void OnDisable()
        {
            ownerHealth.Damaged -= OnOwnerDamaged;
            ownerHealth.HealthChanged -= OnHealthChanged;
            ownerHealth.Died -= ResetCharge;
        }

        public void Configure(
            float capacity,
            float perDamageDealt,
            float perDamageTaken)
        {
            maximumCharge = Mathf.Max(.01f, capacity);
            chargePerDamageDealt = Mathf.Max(0f, perDamageDealt);
            chargePerDamageTaken = Mathf.Max(0f, perDamageTaken);
            CurrentCharge = Mathf.Min(CurrentCharge, maximumCharge);
            NotifyChargeChanged();
        }

        public void AddCharge(float amount)
        {
            if (amount <= 0f || IsUltimateReady)
                return;

            CurrentCharge = Mathf.Min(maximumCharge, CurrentCharge + amount);
            NotifyChargeChanged();
        }

        public bool TryConsumeUltimate()
        {
            if (!IsUltimateReady)
                return false;

            CurrentCharge = 0f;
            NotifyChargeChanged();
            return true;
        }

        public void ResetCharge()
        {
            if (CurrentCharge <= 0f)
                return;

            CurrentCharge = 0f;
            NotifyChargeChanged();
        }

        public void OnDamageDealt(
            Damageable target,
            DamageInfo damageInfo,
            float appliedDamage)
        {
            AddCharge(appliedDamage * chargePerDamageDealt);
        }

        private void OnOwnerDamaged(DamageInfo damageInfo)
        {
            float appliedDamage = Mathf.Max(0f, previousHealth - ownerHealth.CurrentHealth);
            previousHealth = ownerHealth.CurrentHealth;
            AddCharge(appliedDamage * chargePerDamageTaken);
        }

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            previousHealth = currentHealth;
        }

        private void NotifyChargeChanged()
        {
            ChargeChanged?.Invoke(CurrentCharge, maximumCharge);
        }

        private void OnValidate()
        {
            maximumCharge = Mathf.Max(.01f, maximumCharge);
            chargePerDamageDealt = Mathf.Max(0f, chargePerDamageDealt);
            chargePerDamageTaken = Mathf.Max(0f, chargePerDamageTaken);
        }
    }
}
