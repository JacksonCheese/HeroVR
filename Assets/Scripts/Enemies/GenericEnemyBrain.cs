using HeroVR.Combat;
using HeroVR.Prototype;
using UnityEngine;

namespace HeroVR.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable), typeof(Rigidbody))]
    public sealed class GenericEnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private TrainingBot legacyTrainingBotDriver;
        [SerializeField] private bool disableAutomaticRespawn = true;
        [SerializeField] private Damageable initialTarget;

        private Damageable damageable;
        private Rigidbody body;
        private RagdollController ragdoll;

        public EnemyDefinition Definition => definition;
        public EnemyAttackRole AttackRole => definition != null
            ? definition.AttackRole
            : EnemyAttackRole.Melee;
        public Damageable Health => damageable;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
            body = GetComponent<Rigidbody>();
            ragdoll = GetComponent<RagdollController>();
            RespawnOnDeath respawn = GetComponent<RespawnOnDeath>();
            if (disableAutomaticRespawn && respawn != null)
                respawn.enabled = false;
            if (legacyTrainingBotDriver == null)
                legacyTrainingBotDriver = GetComponent<TrainingBot>();
            ApplyDefinition();
            if (initialTarget != null)
                legacyTrainingBotDriver?.SetTarget(initialTarget);
        }

        public void Configure(EnemyDefinition enemyDefinition)
        {
            definition = enemyDefinition;
            if (damageable != null)
                ApplyDefinition();
        }

        public void SetTarget(Damageable target)
        {
            initialTarget = target;
            legacyTrainingBotDriver?.SetTarget(target);
        }

        private void ApplyDefinition()
        {
            if (definition == null)
                return;

            damageable.SetMaxHealth(definition.MaximumHealth);
            body.mass = definition.BodyMass;
            legacyTrainingBotDriver?.ConfigureFromDefinition(definition);
            ragdoll?.Configure(
                definition.RagdollImpactThreshold,
                true,
                definition.RecoversFromRagdoll,
                definition.RagdollRecoveryDelay,
                .35f,
                definition.CorpseCleanupDelay,
                8);
        }
    }
}
