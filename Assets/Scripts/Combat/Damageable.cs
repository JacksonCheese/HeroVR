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

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageInfo.Amount);
            Damaged?.Invoke(damageInfo);
            NotifyHealthChanged();

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
