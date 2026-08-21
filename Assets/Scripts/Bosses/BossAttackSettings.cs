using System;
using UnityEngine;

namespace HeroVR.Bosses
{
    [Serializable]
    public struct BossAttackSettings
    {
        public BossAttackSettings(
            BossAttackType attackType,
            float cooldown,
            float telegraphDelay,
            float range,
            float damage,
            float knockbackImpulse)
        {
            this.attackType = attackType;
            this.cooldown = cooldown;
            this.telegraphDelay = telegraphDelay;
            this.range = range;
            this.damage = damage;
            this.knockbackImpulse = knockbackImpulse;
        }

        public BossAttackType attackType;
        [Min(0f)] public float cooldown;
        [Min(0f)] public float telegraphDelay;
        [Min(0f)] public float range;
        [Min(0f)] public float damage;
        [Min(0f)] public float knockbackImpulse;
    }
}
