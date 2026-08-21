using System;
using System.Collections.Generic;
using HeroVR.Gameplay;
using UnityEngine;
using UnityEngine.AI;

namespace HeroVR.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable), typeof(Rigidbody))]
    public sealed class RagdollController : MonoBehaviour
    {
        [Header("Activation")]
        [SerializeField, Min(0f)] private float activationImpactStrength = 18f;
        [SerializeField] private bool ragdollOnDeath = true;

        [Header("Rig")]
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody locomotionBody;
        [SerializeField] private Rigidbody[] ragdollBodies;
        [SerializeField] private Collider[] ragdollColliders;

        [Header("Lifecycle")]
        [SerializeField] private bool recoverWhenAlive = true;
        [SerializeField, Min(0f)] private float recoveryDelay = 3f;
        [SerializeField, Min(0f)] private float recoveryDuration = .35f;
        [SerializeField, Min(0f)] private float corpseSettleDelay = 8f;
        [SerializeField, Min(1)] private int maxActiveRagdolls = 8;

        private static readonly List<RagdollController> ActiveRagdolls =
            new List<RagdollController>();

        private Damageable damageable;
        private RespawnOnDeath respawn;
        private IControlSuspendable[] controlReceivers;
        private bool[] originalKinematicStates;
        private bool[] originalGravityStates;
        private bool[] originalColliderStates;
        private RigidbodyConstraints originalLocomotionConstraints;
        private float nextStateTime;
        private bool settled;

        public RagdollState State { get; private set; } = RagdollState.Animated;
        public bool IsRagdolled => State != RagdollState.Animated;
        public float ActivationImpactStrength => activationImpactStrength;
        public DamageInfo LastActivationDamage { get; private set; }
        public IReadOnlyList<Rigidbody> RagdollBodies => ragdollBodies;

        public event Action<RagdollState> StateChanged;
        public event Action<DamageInfo> RagdollActivated;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
            respawn = GetComponent<RespawnOnDeath>();
            if (locomotionBody == null)
                locomotionBody = GetComponent<Rigidbody>();

            DiscoverRigIfNeeded();
            CaptureOriginalRigState();
            DiscoverControlReceivers();
            RestoreAnimatedRig();
        }

        private void OnEnable()
        {
            damageable.Damaged += OnDamaged;
            damageable.Died += OnDied;
            if (respawn != null)
                respawn.Respawned += ResetRagdoll;
        }

        private void OnDisable()
        {
            damageable.Damaged -= OnDamaged;
            damageable.Died -= OnDied;
            if (respawn != null)
                respawn.Respawned -= ResetRagdoll;

            ActiveRagdolls.Remove(this);
            SetControlSuspended(false);
            if (ragdollBodies != null && originalKinematicStates != null)
                RestoreAnimatedRig();
            State = RagdollState.Animated;
        }

        private void Update()
        {
            if (State == RagdollState.FullRagdoll && Time.time >= nextStateTime)
            {
                if (!damageable.IsDead && recoverWhenAlive)
                    BeginRecovery();
                else if (damageable.IsDead && corpseSettleDelay > 0f && !settled)
                    SettleRagdoll();
            }
            else if (State == RagdollState.Recovering && Time.time >= nextStateTime)
            {
                RestoreAnimatedState();
            }
        }

        public void Configure(
            float impactThreshold,
            bool activateOnDeath,
            bool allowRecovery,
            float delayBeforeRecovery,
            float recoveryTime,
            float settleDelay,
            int activeLimit)
        {
            activationImpactStrength = Mathf.Max(0f, impactThreshold);
            ragdollOnDeath = activateOnDeath;
            recoverWhenAlive = allowRecovery;
            recoveryDelay = Mathf.Max(0f, delayBeforeRecovery);
            recoveryDuration = Mathf.Max(0f, recoveryTime);
            corpseSettleDelay = Mathf.Max(0f, settleDelay);
            maxActiveRagdolls = Mathf.Max(1, activeLimit);
        }

        public bool TryActivateFromImpact(DamageInfo damageInfo)
        {
            if (damageInfo.ImpactStrength < activationImpactStrength)
                return false;

            ForceRagdoll(damageInfo);
            return true;
        }

        public void ForceRagdoll(DamageInfo damageInfo = default)
        {
            LastActivationDamage = damageInfo;
            if (State == RagdollState.FullRagdoll)
            {
                ScheduleRagdollLifecycle();
                return;
            }

            State = RagdollState.FullRagdoll;
            settled = false;
            if (animator != null)
                animator.enabled = false;

            SetControlSuspended(true);
            EnableRagdollPhysics();
            RegisterActiveRagdoll();
            ScheduleRagdollLifecycle();
            StateChanged?.Invoke(State);
            RagdollActivated?.Invoke(damageInfo);
        }

        public void BeginRecovery()
        {
            if (State != RagdollState.FullRagdoll || damageable.IsDead)
                return;

            State = RagdollState.Recovering;
            nextStateTime = Time.time + recoveryDuration;
            StateChanged?.Invoke(State);
        }

        public void ResetRagdoll()
        {
            ActiveRagdolls.Remove(this);
            RestoreAnimatedRig();
            SetControlSuspended(false);
            settled = false;
            nextStateTime = 0f;

            if (State != RagdollState.Animated)
            {
                State = RagdollState.Animated;
                StateChanged?.Invoke(State);
            }
        }

        public Rigidbody GetClosestBody(Vector3 worldPoint)
        {
            Rigidbody closest = locomotionBody;
            float closestDistance = float.PositiveInfinity;
            for (int index = 0; index < ragdollBodies.Length; index++)
            {
                Rigidbody candidate = ragdollBodies[index];
                if (candidate == null)
                    continue;

                float distance = (candidate.worldCenterOfMass - worldPoint).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = candidate;
                }
            }

            return closest;
        }

        private void OnDamaged(DamageInfo damageInfo)
        {
            LastActivationDamage = damageInfo;
            TryActivateFromImpact(damageInfo);
        }

        private void OnDied()
        {
            // A zero-delay RespawnOnDeath subscriber may already have restored
            // health earlier in the same death event dispatch.
            if (ragdollOnDeath && damageable.IsDead)
                ForceRagdoll(LastActivationDamage);
        }

        private void ScheduleRagdollLifecycle()
        {
            float delay = damageable.IsDead ? corpseSettleDelay : recoveryDelay;
            nextStateTime = delay > 0f ? Time.time + delay : Time.time;
        }

        private void RestoreAnimatedState()
        {
            ActiveRagdolls.Remove(this);
            RestoreAnimatedRig();
            SetControlSuspended(false);
            settled = false;
            State = RagdollState.Animated;
            StateChanged?.Invoke(State);
        }

        private void EnableRagdollPhysics()
        {
            bool rootOnly = ragdollBodies.Length == 1 &&
                ragdollBodies[0] == locomotionBody;
            for (int index = 0; index < ragdollBodies.Length; index++)
            {
                Rigidbody body = ragdollBodies[index];
                if (body == null)
                    continue;

                body.isKinematic = false;
                body.useGravity = true;
                body.WakeUp();
            }

            for (int index = 0; index < ragdollColliders.Length; index++)
            {
                if (ragdollColliders[index] != null)
                    ragdollColliders[index].enabled = true;
            }

            if (rootOnly && locomotionBody != null)
                locomotionBody.constraints = RigidbodyConstraints.None;
        }

        private void RestoreAnimatedRig()
        {
            for (int index = 0; index < ragdollBodies.Length; index++)
            {
                Rigidbody body = ragdollBodies[index];
                if (body == null)
                    continue;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = originalKinematicStates[index];
                body.useGravity = originalGravityStates[index];
            }

            for (int index = 0; index < ragdollColliders.Length; index++)
            {
                if (ragdollColliders[index] != null)
                    ragdollColliders[index].enabled = originalColliderStates[index];
            }

            if (locomotionBody != null)
                locomotionBody.constraints = originalLocomotionConstraints;
            if (animator != null)
                animator.enabled = true;
        }

        private void SettleRagdoll()
        {
            settled = true;
            ActiveRagdolls.Remove(this);
            for (int index = 0; index < ragdollBodies.Length; index++)
            {
                Rigidbody body = ragdollBodies[index];
                if (body == null)
                    continue;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
        }

        private void RegisterActiveRagdoll()
        {
            ActiveRagdolls.Remove(this);
            ActiveRagdolls.Add(this);
            while (ActiveRagdolls.Count > maxActiveRagdolls)
            {
                RagdollController oldest = ActiveRagdolls[0];
                ActiveRagdolls.RemoveAt(0);
                if (oldest != null && oldest != this)
                    oldest.SettleRagdoll();
            }
        }

        private void DiscoverRigIfNeeded()
        {
            if (ragdollBodies == null || ragdollBodies.Length == 0)
            {
                Rigidbody[] discovered = GetComponentsInChildren<Rigidbody>(true);
                if (discovered.Length > 1)
                {
                    List<Rigidbody> childBodies = new List<Rigidbody>();
                    for (int index = 0; index < discovered.Length; index++)
                    {
                        if (discovered[index] != locomotionBody)
                            childBodies.Add(discovered[index]);
                    }

                    ragdollBodies = childBodies.Count > 0
                        ? childBodies.ToArray()
                        : new[] { locomotionBody };
                }
                else
                {
                    ragdollBodies = new[] { locomotionBody };
                }
            }

            if (ragdollColliders == null || ragdollColliders.Length == 0)
            {
                List<Collider> colliders = new List<Collider>();
                for (int index = 0; index < ragdollBodies.Length; index++)
                {
                    if (ragdollBodies[index] == null)
                        continue;
                    Collider[] bodyColliders =
                        ragdollBodies[index].GetComponents<Collider>();
                    colliders.AddRange(bodyColliders);
                }

                ragdollColliders = colliders.ToArray();
            }
        }

        private void CaptureOriginalRigState()
        {
            originalKinematicStates = new bool[ragdollBodies.Length];
            originalGravityStates = new bool[ragdollBodies.Length];
            for (int index = 0; index < ragdollBodies.Length; index++)
            {
                Rigidbody body = ragdollBodies[index];
                originalKinematicStates[index] = body != null && body.isKinematic;
                originalGravityStates[index] = body != null && body.useGravity;
            }

            originalColliderStates = new bool[ragdollColliders.Length];
            for (int index = 0; index < ragdollColliders.Length; index++)
            {
                originalColliderStates[index] =
                    ragdollColliders[index] != null && ragdollColliders[index].enabled;
            }

            originalLocomotionConstraints = locomotionBody != null
                ? locomotionBody.constraints
                : RigidbodyConstraints.None;
        }

        private void DiscoverControlReceivers()
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            List<IControlSuspendable> receivers = new List<IControlSuspendable>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IControlSuspendable receiver)
                    receivers.Add(receiver);
            }

            controlReceivers = receivers.ToArray();
        }

        private void SetControlSuspended(bool suspended)
        {
            for (int index = 0; index < controlReceivers.Length; index++)
                controlReceivers[index].SetControlSuspended(this, suspended);
        }

        private void OnValidate()
        {
            activationImpactStrength = Mathf.Max(0f, activationImpactStrength);
            recoveryDelay = Mathf.Max(0f, recoveryDelay);
            recoveryDuration = Mathf.Max(0f, recoveryDuration);
            corpseSettleDelay = Mathf.Max(0f, corpseSettleDelay);
            maxActiveRagdolls = Mathf.Max(1, maxActiveRagdolls);
        }
    }
}
