using UnityEngine;

namespace HeroVR.Combat
{
    public static class CombatHitResolver
    {
        public static int Apply(Collider hitCollider, DamageInfo damageInfo)
        {
            if (hitCollider == null)
                return 0;

            Transform current = hitCollider.transform;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                int receiverCount = 0;
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is ICombatDamageReceiver receiver &&
                        receiver.TryReceiveDamage(damageInfo))
                    {
                        receiverCount++;
                    }
                }

                // The nearest receiver level owns this hit. This lets a boss hit
                // region modify a hit before it reaches the shared root health.
                if (receiverCount > 0)
                    return receiverCount;

                current = current.parent;
            }

            return 0;
        }
    }
}
