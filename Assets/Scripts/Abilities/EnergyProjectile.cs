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
        private Collider[] projectileColliders;
        private Transform ownerRoot;

        public GameObject Owner { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            projectileColliders = GetComponentsInChildren<Collider>();
        }

        public void Launch(Vector3 velocity, GameObject owner = null)
        {
            SetOwner(owner);
            rb.linearVelocity = velocity;
            Destroy(gameObject, lifetime);
        }

        public void ConfigureCombat(
            float damageAmount,
            float duration,
            float impulse)
        {
            damage = Mathf.Max(0f, damageAmount);
            lifetime = Mathf.Max(.01f, duration);
            knockbackImpulse = Mathf.Max(0f, impulse);
        }

        public void SetOwner(GameObject owner)
        {
            Owner = owner;
            ownerRoot = owner != null ? owner.transform.root : null;

            if (ownerRoot == null)
                return;

            Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>();
            foreach (Collider projectileCollider in projectileColliders)
            {
                foreach (Collider ownerCollider in ownerColliders)
                    Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (ownerRoot != null && collision.transform.root == ownerRoot)
                return;

            Vector3 direction = rb.linearVelocity.sqrMagnitude > 0.01f
                ? rb.linearVelocity.normalized
                : transform.forward;

            var target = collision.collider.GetComponentInParent<Damageable>();
            if (target != null && target.transform.root != ownerRoot)
            {
                Vector3 hitPoint = collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : transform.position;

                target.TakeDamage(new DamageInfo(
                    damage,
                    Owner,
                    hitPoint,
                    direction,
                    knockbackImpulse));
            }

            if (collision.rigidbody != null && !collision.rigidbody.isKinematic)
                collision.rigidbody.AddForce(direction * knockbackImpulse, ForceMode.Impulse);

            Destroy(gameObject);
        }
    }
}
