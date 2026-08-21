using System;
using UnityEngine;

namespace HeroVR.Bosses
{
    [CreateAssetMenu(
        fileName = "BossDefinition",
        menuName = "HeroVR/Boss Definition")]
    public sealed class BossDefinition : ScriptableObject
    {
        [SerializeField] private string bossId = "placeholder-giant";
        [SerializeField] private string displayName = "Placeholder Giant";
        [SerializeField, Min(.01f)] private float maximumHealth = 1200f;
        [SerializeField, Min(1f)] private float physicalScale = 4f;
        [SerializeField, Min(0f)] private float movementSpeed = 2f;
        [SerializeField] private BossAttackSettings[] attacks =
        {
            new BossAttackSettings(BossAttackType.Stomp, 3f, .75f, 6f, 30f, 24f)
        };
        [SerializeField] private BossPhaseSettings[] phases =
        {
            new BossPhaseSettings(.75f, 2, 0),
            new BossPhaseSettings(.4f, 3, 1)
        };

        public string BossId => bossId;
        public string DisplayName => displayName;
        public float MaximumHealth => maximumHealth;
        public float PhysicalScale => physicalScale;
        public float MovementSpeed => movementSpeed;
        public BossAttackSettings[] Attacks => attacks;
        public BossPhaseSettings[] Phases => phases;

        public void Configure(
            string id,
            string bossName,
            float health,
            float scale,
            float speed,
            BossAttackSettings[] attackSlots,
            BossPhaseSettings[] phaseSettings)
        {
            bossId = string.IsNullOrWhiteSpace(id) ? "boss" : id.Trim();
            displayName = string.IsNullOrWhiteSpace(bossName)
                ? "Boss"
                : bossName.Trim();
            maximumHealth = Mathf.Max(.01f, health);
            physicalScale = Mathf.Max(1f, scale);
            movementSpeed = Mathf.Max(0f, speed);
            attacks = attackSlots ?? Array.Empty<BossAttackSettings>();
            phases = phaseSettings ?? Array.Empty<BossPhaseSettings>();
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(.01f, maximumHealth);
            physicalScale = Mathf.Max(1f, physicalScale);
            movementSpeed = Mathf.Max(0f, movementSpeed);
        }
    }
}
