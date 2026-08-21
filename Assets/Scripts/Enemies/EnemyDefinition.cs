using UnityEngine;

namespace HeroVR.Enemies
{
    [CreateAssetMenu(
        fileName = "EnemyDefinition",
        menuName = "HeroVR/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string enemyId = "basic-minion";
        [SerializeField] private string displayName = "Basic Minion";
        [SerializeField] private EnemyAttackRole attackRole = EnemyAttackRole.Melee;
        [SerializeField, Min(.01f)] private float maximumHealth = 100f;
        [SerializeField, Min(.01f)] private float bodyMass = 2.5f;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float acceleration = 12f;
        [SerializeField, Min(.1f)] private float attackRange = 1.75f;
        [SerializeField, Min(0f)] private float attackDamage = 12f;
        [SerializeField, Min(0f)] private float attackKnockback = 7f;
        [SerializeField, Min(0f)] private float ragdollImpactThreshold = 18f;
        [SerializeField] private bool recoversFromRagdoll = true;
        [SerializeField, Min(0f)] private float ragdollRecoveryDelay = 3f;
        [SerializeField, Min(0f)] private float corpseCleanupDelay = 8f;

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public EnemyAttackRole AttackRole => attackRole;
        public float MaximumHealth => maximumHealth;
        public float BodyMass => bodyMass;
        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float AttackKnockback => attackKnockback;
        public float RagdollImpactThreshold => ragdollImpactThreshold;
        public bool RecoversFromRagdoll => recoversFromRagdoll;
        public float RagdollRecoveryDelay => ragdollRecoveryDelay;
        public float CorpseCleanupDelay => corpseCleanupDelay;

        public void Configure(
            string id,
            string enemyName,
            EnemyAttackRole role,
            float health,
            float mass,
            float speed,
            float movementAcceleration,
            float range,
            float damage,
            float knockback,
            float ragdollThreshold,
            bool recover,
            float recoveryDelay,
            float cleanupDelay)
        {
            enemyId = string.IsNullOrWhiteSpace(id) ? "enemy" : id.Trim();
            displayName = string.IsNullOrWhiteSpace(enemyName)
                ? "Enemy"
                : enemyName.Trim();
            attackRole = role;
            maximumHealth = Mathf.Max(.01f, health);
            bodyMass = Mathf.Max(.01f, mass);
            moveSpeed = Mathf.Max(0f, speed);
            acceleration = Mathf.Max(0f, movementAcceleration);
            attackRange = Mathf.Max(.1f, range);
            attackDamage = Mathf.Max(0f, damage);
            attackKnockback = Mathf.Max(0f, knockback);
            ragdollImpactThreshold = Mathf.Max(0f, ragdollThreshold);
            recoversFromRagdoll = recover;
            ragdollRecoveryDelay = Mathf.Max(0f, recoveryDelay);
            corpseCleanupDelay = Mathf.Max(0f, cleanupDelay);
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(.01f, maximumHealth);
            bodyMass = Mathf.Max(.01f, bodyMass);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            attackRange = Mathf.Max(.1f, attackRange);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackKnockback = Mathf.Max(0f, attackKnockback);
            ragdollImpactThreshold = Mathf.Max(0f, ragdollImpactThreshold);
            ragdollRecoveryDelay = Mathf.Max(0f, ragdollRecoveryDelay);
            corpseCleanupDelay = Mathf.Max(0f, corpseCleanupDelay);
        }
    }
}
