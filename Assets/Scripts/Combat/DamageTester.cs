using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.Combat
{
    public class DamageTester : MonoBehaviour
    {
        [SerializeField] private Key damageKey = Key.T;

        private Damageable damageable;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
        }

        private void Update()
        {
            if (Keyboard.current == null ||
                !Keyboard.current[damageKey].wasPressedThisFrame)
                return;

            damageable.TakeDamage(10f);
            Debug.Log($"Dummy Health: {damageable.CurrentHealth}");
        }
    }
}
