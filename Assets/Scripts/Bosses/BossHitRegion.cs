using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Bosses
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class BossHitRegion : MonoBehaviour, ICombatDamageReceiver
    {
        [SerializeField] private BossController boss;
        [SerializeField] private BossHitRegionType regionType = BossHitRegionType.Torso;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        public BossController Boss => boss;
        public BossHitRegionType RegionType => regionType;
        public float DamageMultiplier => damageMultiplier;

        private void Awake()
        {
            if (boss == null)
                boss = GetComponentInParent<BossController>();
        }

        public void Configure(
            BossController owner,
            BossHitRegionType type,
            float multiplier)
        {
            boss = owner;
            regionType = type;
            damageMultiplier = Mathf.Max(0f, multiplier);
        }

        public bool TryReceiveDamage(DamageInfo damageInfo)
        {
            return boss != null &&
                boss.TryReceiveRegionDamage(
                    this,
                    damageInfo.Scaled(damageMultiplier));
        }

        private void OnValidate()
        {
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
        }
    }
}
