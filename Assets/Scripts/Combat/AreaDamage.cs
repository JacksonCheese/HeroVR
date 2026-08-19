using System.Collections.Generic;
using UnityEngine;

namespace HeroVR.Combat
{
    public static class AreaDamage
    {
        public static int Apply(
            Vector3 center,
            float radius,
            float damage,
            float knockbackImpulse,
            GameObject instigator,
            Collider[] overlapBuffer,
            HashSet<Damageable> damageTargets,
            HashSet<Rigidbody> physicsTargets,
            int layerMask = Physics.AllLayers)
        {
            if (overlapBuffer == null || overlapBuffer.Length == 0)
                return 0;

            damageTargets.Clear();
            physicsTargets.Clear();

            Transform instigatorRoot = instigator != null
                ? instigator.transform.root
                : null;

            int overlapCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                overlapBuffer,
                layerMask,
                QueryTriggerInteraction.Ignore);

            int damagedTargetCount = 0;
            for (int index = 0; index < overlapCount; index++)
            {
                Collider hit = overlapBuffer[index];
                if (hit == null || hit.transform.root == instigatorRoot)
                    continue;

                Rigidbody body = hit.attachedRigidbody;
                Damageable target = hit.GetComponentInParent<Damageable>();
                Vector3 targetPosition = body != null
                    ? body.worldCenterOfMass
                    : target != null
                        ? target.transform.position
                        : hit.bounds.center;

                Vector3 direction = targetPosition - center;
                direction.y = Mathf.Max(direction.y, .35f);
                direction.Normalize();

                if (target != null && !target.IsDead && damageTargets.Add(target))
                {
                    target.TakeDamage(new DamageInfo(
                        damage,
                        instigator,
                        hit.ClosestPoint(center),
                        direction,
                        knockbackImpulse));
                    damagedTargetCount++;
                }

                if (body != null &&
                    !body.isKinematic &&
                    physicsTargets.Add(body))
                {
                    body.AddForce(direction * knockbackImpulse, ForceMode.Impulse);
                }
            }

            return damagedTargetCount;
        }
    }
}
