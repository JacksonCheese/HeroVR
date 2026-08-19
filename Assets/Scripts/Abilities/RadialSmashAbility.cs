using System.Collections.Generic;
using UnityEngine;
using HeroVR.Combat;

namespace HeroVR.Abilities
{
    public sealed class RadialSmashAbility : HeroAbility
    {
        [SerializeField] private Transform centerPoint;
        [SerializeField, Min(0f)] private float radius = 4f;
        [SerializeField, Min(0f)] private float damage = 32f;
        [SerializeField, Min(0f)] private float knockbackImpulse = 14f;

        private readonly Collider[] hitBuffer = new Collider[64];
        private readonly HashSet<Damageable> damageTargets = new HashSet<Damageable>();
        private readonly HashSet<Rigidbody> physicsTargets = new HashSet<Rigidbody>();

        public int LastDamagedTargetCount { get; private set; }

        public void SetCenterPoint(Transform center)
        {
            centerPoint = center;
        }

        public void ConfigureCombat(
            float attackRadius,
            float damageAmount,
            float impulse)
        {
            radius = Mathf.Max(0f, attackRadius);
            damage = Mathf.Max(0f, damageAmount);
            knockbackImpulse = Mathf.Max(0f, impulse);
        }

        protected override bool Activate()
        {
            Vector3 center = centerPoint != null
                ? centerPoint.position
                : transform.position;

            LastDamagedTargetCount = AreaDamage.Apply(
                center,
                radius,
                damage,
                knockbackImpulse,
                Owner,
                hitBuffer,
                damageTargets,
                physicsTargets);

            return true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            radius = Mathf.Max(0f, radius);
            damage = Mathf.Max(0f, damage);
            knockbackImpulse = Mathf.Max(0f, knockbackImpulse);
        }
    }
}
