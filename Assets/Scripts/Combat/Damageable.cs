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

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            onHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
                onDeath?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            onHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            onHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
