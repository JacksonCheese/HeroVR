using System;
using UnityEngine;
using UnityEngine.Events;

namespace HeroVR.Combat
{
    public class Damageable : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private UnityEvent<float, float> onHealthChanged;
        [SerializeField] private UnityEvent onDeath;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<DamageInfo> Damaged;
        public event Action<CombatImpact> ImpactReceived;
        public event Action<float, float> HealthChanged;
        public event Action Died;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(new DamageInfo(amount));
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (IsDead || damageInfo.Amount <= 0f)
                return;

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageInfo.Amount);
            float appliedDamage = previousHealth - CurrentHealth;
            Damaged?.Invoke(damageInfo);
            ImpactReceived?.Invoke(new CombatImpact(this, damageInfo, appliedDamage));
            NotifyHealthChanged();
            NotifyDamageDealt(damageInfo, appliedDamage);

            if (CurrentHealth <= 0f)
            {
                Died?.Invoke();
                onDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            NotifyHealthChanged();
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            NotifyHealthChanged();
        }

        public void SetMaxHealth(float value, bool refill = true)
        {
            maxHealth = Mathf.Max(.01f, value);
            CurrentHealth = refill
                ? maxHealth
                : Mathf.Min(CurrentHealth, maxHealth);
            NotifyHealthChanged();
        }

        private void NotifyDamageDealt(DamageInfo damageInfo, float appliedDamage)
        {
            if (damageInfo.Instigator == null || appliedDamage <= 0f)
                return;

            Transform instigatorRoot = damageInfo.Instigator.transform.root;
            MonoBehaviour[] behaviours = instigatorRoot.GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IDamageDealtReceiver receiver)
                    receiver.OnDamageDealt(this, damageInfo, appliedDamage);
            }
        }

        private void NotifyHealthChanged()
        {
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            onHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(.01f, maxHealth);
        }
    }
}
