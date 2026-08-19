using UnityEngine;

namespace HeroVR.Combat
{
    public class DamageTester : MonoBehaviour
    {
        private Damageable damageable;

        void Start()
        {
            damageable = GetComponent<Damageable>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                damageable.TakeDamage(10f);

                Debug.Log(
                    "Dummy Health: " +
                    damageable.CurrentHealth
                );
            }
        }
    }
}