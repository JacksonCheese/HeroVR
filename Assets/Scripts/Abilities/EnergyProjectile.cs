using UnityEngine;
using HeroVR.Combat;

namespace HeroVR.Abilities
{
    [RequireComponent(typeof(Rigidbody))]
    public class EnergyProjectile : MonoBehaviour
    {
        [SerializeField] private float damage = 25f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float knockbackImpulse = 6f;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            Destroy(gameObject, lifetime);
        }

        public void Launch(Vector3 velocity)
        {
            rb.linearVelocity = velocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            var target = collision.collider.GetComponentInParent<Damageable>();
            if (target != null)
                target.TakeDamage(damage);

            if (collision.rigidbody != null && !collision.rigidbody.isKinematic)
            {
                Vector3 direction = rb.linearVelocity.sqrMagnitude > 0.01f
                    ? rb.linearVelocity.normalized
                    : transform.forward;

                collision.rigidbody.AddForce(direction * knockbackImpulse, ForceMode.Impulse);
            }

            Destroy(gameObject);
        }
    }
}
