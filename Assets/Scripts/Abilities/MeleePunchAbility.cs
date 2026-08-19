using UnityEngine;
using HeroVR.Combat;

namespace HeroVR.Abilities
{
    public sealed class MeleePunchAbility : HeroAbility
    {
        [SerializeField] private Transform attackOrigin;
        [SerializeField, Min(0f)] private float damage = 22f;
        [SerializeField, Min(0f)] private float range = 1.6f;
        [SerializeField, Min(0f)] private float radius = .65f;
        [SerializeField, Min(0f)] private float knockbackImpulse = 9f;

        private readonly Collider[] hitBuffer = new Collider[32];

        public void SetAttackOrigin(Transform origin)
        {
            attackOrigin = origin;
        }

        protected override bool Activate()
        {
            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Vector3 direction = origin.forward;
            Vector3 center = origin.position + direction * range;
            Transform ownerRoot = Owner.transform.root;

            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                hitBuffer,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < hitCount; index++)
            {
                Collider hit = hitBuffer[index];
                if (hit == null || hit.transform.root == ownerRoot)
                    continue;

                Damageable target = hit.GetComponentInParent<Damageable>();
                if (target == null || target.IsDead)
                    continue;

                target.TakeDamage(new DamageInfo(
                    damage,
                    Owner,
                    hit.ClosestPoint(center),
                    direction,
                    knockbackImpulse));

                Rigidbody body = hit.attachedRigidbody;
                if (body != null && !body.isKinematic)
                    body.AddForce(direction * knockbackImpulse, ForceMode.Impulse);

                break;
            }

            return true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            damage = Mathf.Max(0f, damage);
            range = Mathf.Max(0f, range);
            radius = Mathf.Max(0f, radius);
            knockbackImpulse = Mathf.Max(0f, knockbackImpulse);
        }
    }
}
