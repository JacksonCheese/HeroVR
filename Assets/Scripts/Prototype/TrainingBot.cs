using UnityEngine;
using HeroVR.Combat;

namespace HeroVR.Prototype
{
    [RequireComponent(typeof(Rigidbody), typeof(Damageable))]
    public class TrainingBot : MonoBehaviour
    {
        public float moveSpeed = 5f, attackRange = 1.75f, attackDamage = 12f;
        Rigidbody rb;
        Damageable health;
        DesktopHeroController target;
        Vector3 spawn;
        Quaternion spawnRot;
        float nextAttack, deadSince = -1f;

        public Damageable Health => health;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            health = GetComponent<Damageable>();
            spawn = transform.position;
            spawnRot = transform.rotation;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearDamping = 4f;
        }

        public void SetTarget(DesktopHeroController player) => target = player;

        void FixedUpdate()
        {
            if (target == null) return;

            if (health.IsDead)
            {
                rb.linearVelocity = Vector3.zero;
                if (deadSince < 0) deadSince = Time.time;
                if (Time.time - deadSince >= 2f)
                {
                    transform.SetPositionAndRotation(spawn, spawnRot);
                    health.ResetHealth();
                    deadSince = -1;
                }
                return;
            }

            Vector3 to = target.transform.position - transform.position;
            to.y = 0;
            float distance = to.magnitude;

            if (distance > attackRange && distance > .01f)
            {
                Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                Vector3 desired = to.normalized * moveSpeed;
                rb.AddForce((desired - horizontal) * 12f, ForceMode.Acceleration);
            }

            if (distance <= attackRange && Time.time >= nextAttack)
            {
                nextAttack = Time.time + .9f;
                target.Health.TakeDamage(attackDamage);
            }

            if (to.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(to.normalized), 8f * Time.fixedDeltaTime);
        }
    }
}