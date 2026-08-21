using System;
using System.Collections.Generic;
using HeroVR.Combat;
using HeroVR.Enemies;
using UnityEngine;

namespace HeroVR.Bosses
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable))]
    public sealed class BossController : MonoBehaviour, IOpponentReceiver
    {
        [SerializeField] private BossDefinition definition;
        [SerializeField] private MinionSpawnController minionSpawner;
        [SerializeField] private bool attacksEnabled = true;

        private readonly Collider[] overlapBuffer = new Collider[64];
        private readonly HashSet<Damageable> damageTargets =
            new HashSet<Damageable>();
        private readonly HashSet<Rigidbody> physicsTargets =
            new HashSet<Rigidbody>();
        private readonly HashSet<Transform> receiverTargets =
            new HashSet<Transform>();
        private Damageable health;
        private Damageable target;
        private bool[] triggeredPhases = Array.Empty<bool>();
        private int nextAttackIndex;
        private float nextAttackTime;
        private float resolveAttackTime = -1f;
        private BossAttackSettings pendingAttack;
        private bool deathReported;

        public BossDefinition Definition => definition;
        public Damageable Health => health;
        public Damageable Target => target;
        public int CurrentPhase { get; private set; }
        public bool IsAttackWindingUp => resolveAttackTime >= 0f;
        public BossAttackSettings PendingAttack => pendingAttack;

        public event Action<int> PhaseChanged;
        public event Action<BossAttackSettings> AttackTelegraphed;
        public event Action<BossAttackSettings> AttackResolved;
        public event Action<BossHitRegion, DamageInfo> RegionDamaged;
        public event Action<int, int> MinionSummonRequested;
        public event Action BossDied;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            ApplyDefinition(true);
        }

        private void OnEnable()
        {
            health.HealthChanged += OnHealthChanged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.HealthChanged -= OnHealthChanged;
            health.Died -= OnDied;
        }

        private void Update()
        {
            if (health.IsDead || target == null || target.IsDead || !attacksEnabled)
                return;

            if (IsAttackWindingUp)
            {
                if (Time.time >= resolveAttackTime)
                    ResolvePendingAttack();
                return;
            }

            BossAttackSettings[] attacks = definition != null
                ? definition.Attacks
                : null;
            if (attacks == null || attacks.Length == 0 || Time.time < nextAttackTime)
                return;

            BossAttackSettings candidate = attacks[nextAttackIndex % attacks.Length];
            if (HorizontalDistanceToTarget() <= candidate.range)
                BeginAttack(candidate);
        }

        public void Configure(
            BossDefinition bossDefinition,
            MinionSpawnController summonController = null,
            bool enableAttacks = true)
        {
            definition = bossDefinition;
            minionSpawner = summonController;
            attacksEnabled = enableAttacks;
            if (health != null)
                ApplyDefinition(true);
        }

        public void SetOpponent(Damageable opponent)
        {
            target = opponent;
            minionSpawner?.SetTarget(opponent);
        }

        public bool TryReceiveRegionDamage(
            BossHitRegion region,
            DamageInfo modifiedDamage)
        {
            if (health.IsDead || modifiedDamage.Amount <= 0f)
                return false;

            health.TakeDamage(modifiedDamage);
            RegionDamaged?.Invoke(region, modifiedDamage);
            return true;
        }

        public void ResetEncounter()
        {
            health.ResetHealth();
            CurrentPhase = 0;
            triggeredPhases = definition != null
                ? new bool[definition.Phases.Length]
                : Array.Empty<bool>();
            nextAttackIndex = 0;
            nextAttackTime = Time.time + .5f;
            resolveAttackTime = -1f;
            deathReported = false;
            minionSpawner?.ResetSpawner(true);
        }

        private void ApplyDefinition(bool refillHealth)
        {
            if (definition == null)
            {
                triggeredPhases = Array.Empty<bool>();
                return;
            }

            health.SetMaxHealth(definition.MaximumHealth, refillHealth);
            transform.localScale = Vector3.one * definition.PhysicalScale;
            triggeredPhases = new bool[definition.Phases.Length];
            CurrentPhase = 0;
            deathReported = false;
        }

        private void OnHealthChanged(float current, float maximum)
        {
            if (maximum <= 0f || definition == null)
                return;

            float fraction = current / maximum;
            BossPhaseSettings[] phases = definition.Phases;
            for (int index = 0; index < phases.Length; index++)
            {
                if (triggeredPhases[index] || fraction > phases[index].healthThreshold)
                    continue;

                triggeredPhases[index] = true;
                CurrentPhase++;
                PhaseChanged?.Invoke(CurrentPhase);
                if (phases[index].minionCount > 0)
                {
                    MinionSummonRequested?.Invoke(
                        phases[index].minionCount,
                        phases[index].minionWaveGroup);
                    minionSpawner?.SpawnGroup(
                        phases[index].minionCount,
                        phases[index].minionWaveGroup);
                }
            }
        }

        private void OnDied()
        {
            resolveAttackTime = -1f;
            if (deathReported)
                return;

            deathReported = true;
            BossDied?.Invoke();
        }

        private void BeginAttack(BossAttackSettings attack)
        {
            pendingAttack = attack;
            resolveAttackTime = Time.time + attack.telegraphDelay;
            AttackTelegraphed?.Invoke(attack);
        }

        private void ResolvePendingAttack()
        {
            resolveAttackTime = -1f;
            nextAttackTime = Time.time + pendingAttack.cooldown;
            nextAttackIndex++;

            if (target == null || target.IsDead ||
                HorizontalDistanceToTarget() > pendingAttack.range + .5f)
            {
                return;
            }

            switch (pendingAttack.attackType)
            {
                case BossAttackType.Stomp:
                case BossAttackType.MeleeSwing:
                    AreaDamage.Apply(
                        transform.position,
                        pendingAttack.range,
                        pendingAttack.damage,
                        pendingAttack.knockbackImpulse,
                        gameObject,
                        overlapBuffer,
                        damageTargets,
                        physicsTargets,
                        Physics.AllLayers,
                        receiverTargets,
                        DamageType.Structural);
                    break;
                case BossAttackType.Projectile:
                    // The slot is ranged-ready. A later boss-specific projectile
                    // prefab can consume this same targeting/cooldown contract.
                    target.TakeDamage(new DamageInfo(
                        pendingAttack.damage,
                        gameObject,
                        target.transform.position,
                        (target.transform.position - transform.position).normalized,
                        pendingAttack.knockbackImpulse,
                        pendingAttack.knockbackImpulse,
                        DamageType.Energy));
                    break;
            }

            AttackResolved?.Invoke(pendingAttack);
        }

        private float HorizontalDistanceToTarget()
        {
            Vector3 offset = target.transform.position - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
