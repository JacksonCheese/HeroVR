using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Tests
{
    public sealed class DamageDealtProbe : MonoBehaviour, IDamageDealtReceiver
    {
        public Damageable LastTarget { get; private set; }
        public float LastAppliedDamage { get; private set; }

        public void OnDamageDealt(
            Damageable target,
            DamageInfo damageInfo,
            float appliedDamage)
        {
            LastTarget = target;
            LastAppliedDamage = appliedDamage;
        }
    }
}
