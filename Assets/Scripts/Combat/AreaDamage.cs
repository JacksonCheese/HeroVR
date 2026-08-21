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
            int layerMask = Physics.AllLayers,
            HashSet<Transform> receiverTargets = null,
            DamageType damageType = DamageType.Physical)
        {
            if (overlapBuffer == null || overlapBuffer.Length == 0)
                return 0;

            damageTargets.Clear();
            physicsTargets.Clear();
            receiverTargets?.Clear();

            Transform instigatorRoot = instigator != null
                ? instigator.transform.root
                : null;

            int overlapCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                overlapBuffer,
                layerMask,
                QueryTriggerInteraction.Ignore);
            float impactStrength = Mathf.Max(knockbackImpulse, damage * .25f);

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

                bool damageApplied = false;
                if (target != null && !target.IsDead && damageTargets.Add(target))
                {
                    damageApplied = CombatHitResolver.Apply(
                        hit,
                        new DamageInfo(
                            damage,
                            instigator,
                            hit.ClosestPoint(center),
                            direction,
                            knockbackImpulse,
                            impactStrength,
                            damageType)) > 0;
                }
                else if (target == null && receiverTargets != null)
                {
                    Transform receiverRoot = FindNearestReceiverTransform(hit.transform);
                    if (receiverRoot != null && receiverTargets.Add(receiverRoot))
                    {
                        damageApplied = CombatHitResolver.Apply(
                            hit,
                            new DamageInfo(
                                damage,
                                instigator,
                                hit.ClosestPoint(center),
                                direction,
                                knockbackImpulse,
                                impactStrength,
                                damageType)) > 0;
                    }
                }

                if (damageApplied)
                    damagedTargetCount++;

                if (body != null &&
                    !body.isKinematic &&
                    physicsTargets.Add(body))
                {
                    body.AddForce(direction * knockbackImpulse, ForceMode.Impulse);
                }
            }

            return damagedTargetCount;
        }

        private static Transform FindNearestReceiverTransform(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is ICombatDamageReceiver)
                        return current;
                }
                current = current.parent;
            }
            return null;
        }
    }
}
