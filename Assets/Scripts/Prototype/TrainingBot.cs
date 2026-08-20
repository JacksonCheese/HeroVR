using UnityEngine;
using UnityEngine.AI;
using HeroVR.Combat;

namespace HeroVR.Prototype
{
    [RequireComponent(typeof(Rigidbody), typeof(Damageable), typeof(RespawnOnDeath))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class TrainingBot : MonoBehaviour, IOpponentReceiver
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float turnSpeed = 8f;

        [Header("Navigation")]
        [SerializeField, Min(.1f)] private float repathInterval = .35f;
        [SerializeField, Min(.1f)] private float targetRepathDistance = 1f;
        [SerializeField, Min(.1f)] private float targetSampleDistance = 2f;

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.75f;
        [SerializeField] private float attackDamage = 12f;
        [SerializeField] private float attackKnockbackImpulse = 7f;
        [SerializeField] private float attackWindup = .3f;
        [SerializeField] private float attackCooldown = .9f;
        [SerializeField, Range(1f, 180f)] private float attackFacingAngle = 65f;
        [SerializeField] private LayerMask lineOfSightLayers = ~0;
        [SerializeField] private Color attackTelegraphColor = new Color(1f, .65f, .1f);

        private Rigidbody rb;
        private Damageable health;
        private RespawnOnDeath respawn;
        private NavMeshAgent navMeshAgent;
        private Damageable target;
        private Renderer botRenderer;
        private Color normalColor;
        private float nextAttackTime;
        private float attackHitTime = -1f;
        private float nextRepathTime;
        private Vector3 lastPathTargetPosition = Vector3.positiveInfinity;

        public Damageable Health => health;
        public Damageable Target => target;
        public bool IsAttackWindingUp => attackHitTime >= 0f;
        public bool IsUsingNavMesh =>
            navMeshAgent != null && navMeshAgent.isOnNavMesh && navMeshAgent.hasPath;
        public Vector3 CurrentSteeringDirection { get; private set; }
        public int PathQueryCount { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            health = GetComponent<Damageable>();
            respawn = GetComponent<RespawnOnDeath>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            botRenderer = GetComponentInChildren<Renderer>();

            if (botRenderer != null)
                normalColor = botRenderer.material.color;

            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearDamping = 4f;
            ConfigureNavigationAgent();
        }

        private void OnEnable()
        {
            respawn.Respawned += InvalidatePath;
        }

        private void OnDisable()
        {
            respawn.Respawned -= InvalidatePath;
        }

        public void SetTarget(Damageable combatTarget)
        {
            target = combatTarget;
            InvalidatePath();
        }

        public void SetOpponent(Damageable opponent)
        {
            SetTarget(opponent);
        }

        private void FixedUpdate()
        {
            if (health.IsDead)
            {
                rb.linearVelocity = Vector3.zero;
                CancelPendingAttack();
                return;
            }

            if (target == null || target.IsDead)
            {
                CancelPendingAttack();
                return;
            }

            Vector3 to = target.transform.position - transform.position;
            to.y = 0;
            float distance = to.magnitude;
            Vector3 steeringDirection = GetSteeringDirection(to);
            CurrentSteeringDirection = steeringDirection;

            Vector3 facingDirection =
                IsAttackWindingUp || distance <= attackRange
                    ? to
                    : steeringDirection;

            if (facingDirection.sqrMagnitude > .01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(facingDirection.normalized),
                    turnSpeed * Time.fixedDeltaTime);
            }

            if (IsAttackWindingUp)
            {
                if (Time.time >= attackHitTime)
                    ResolveAttack();
                return;
            }

            if (distance > attackRange && distance > .01f)
            {
                Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                Vector3 desired = steeringDirection * moveSpeed;
                rb.AddForce((desired - horizontal) * acceleration, ForceMode.Acceleration);
                return;
            }

            if (Time.time >= nextAttackTime &&
                IsFacingTarget(to) &&
                HasLineOfSight())
                BeginAttack();
        }

        private void ConfigureNavigationAgent()
        {
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;
            navMeshAgent.autoRepath = false;
            navMeshAgent.autoBraking = false;
            navMeshAgent.obstacleAvoidanceType =
                ObstacleAvoidanceType.NoObstacleAvoidance;
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.acceleration = acceleration;
            navMeshAgent.angularSpeed = 0f;
            navMeshAgent.stoppingDistance = attackRange;
            navMeshAgent.radius = .5f;
            navMeshAgent.height = 2f;
            navMeshAgent.baseOffset = -1f;
        }

        private Vector3 GetSteeringDirection(Vector3 directToTarget)
        {
            Vector3 fallback = directToTarget.sqrMagnitude > .01f
                ? directToTarget.normalized
                : Vector3.zero;

            if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
                return fallback;

            // Rigidbody/knockback owns the Transform. The agent only tracks that
            // position internally and contributes a path corner.
            navMeshAgent.nextPosition = transform.position;
            RefreshPathIfNeeded();

            if (!navMeshAgent.hasPath ||
                navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return fallback;
            }

            Vector3 toSteeringTarget =
                navMeshAgent.steeringTarget - transform.position;
            toSteeringTarget.y = 0f;
            return toSteeringTarget.sqrMagnitude > .01f
                ? toSteeringTarget.normalized
                : fallback;
        }

        private void RefreshPathIfNeeded()
        {
            if (Time.time < nextRepathTime)
                return;

            nextRepathTime = Time.time + repathInterval;
            if (target == null || !navMeshAgent.isOnNavMesh || navMeshAgent.pathPending)
                return;

            Vector3 targetPosition = target.transform.position;
            bool targetMoved =
                (targetPosition - lastPathTargetPosition).sqrMagnitude >=
                targetRepathDistance * targetRepathDistance;
            bool pathNeedsRefresh =
                !navMeshAgent.hasPath ||
                navMeshAgent.isPathStale ||
                navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid;

            if (!targetMoved && !pathNeedsRefresh)
                return;

            if (!NavMesh.SamplePosition(
                    targetPosition,
                    out NavMeshHit targetHit,
                    targetSampleDistance,
                    navMeshAgent.areaMask))
            {
                return;
            }

            PathQueryCount++;
            if (navMeshAgent.SetDestination(targetHit.position))
                lastPathTargetPosition = targetPosition;
        }

        private void InvalidatePath()
        {
            lastPathTargetPosition = Vector3.positiveInfinity;
            nextRepathTime = 0f;
            CurrentSteeringDirection = Vector3.zero;

            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                navMeshAgent.ResetPath();
        }

        private void BeginAttack()
        {
            attackHitTime = Time.time + attackWindup;
            SetTelegraph(true);
        }

        private void ResolveAttack()
        {
            attackHitTime = -1f;
            nextAttackTime = Time.time + attackCooldown;
            SetTelegraph(false);

            if (target == null || target.IsDead)
                return;

            Vector3 to = target.transform.position - transform.position;
            Vector3 horizontalToTarget = new Vector3(to.x, 0f, to.z);
            if (horizontalToTarget.magnitude > attackRange + .2f ||
                !IsFacingTarget(horizontalToTarget) ||
                !HasLineOfSight())
                return;

            Vector3 knockbackDirection =
                (horizontalToTarget.normalized + Vector3.up * .08f).normalized;
            target.TakeDamage(new DamageInfo(
                attackDamage,
                gameObject,
                target.transform.position + Vector3.up * .9f,
                knockbackDirection,
                attackKnockbackImpulse));
        }

        private bool IsFacingTarget(Vector3 direction)
        {
            if (direction.sqrMagnitude <= .01f)
                return true;

            return Vector3.Angle(transform.forward, direction) <= attackFacingAngle;
        }

        private bool HasLineOfSight()
        {
            Vector3 horizontalDirection = target.transform.position - transform.position;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude <= .01f)
                return true;

            horizontalDirection.Normalize();
            Vector3 origin = transform.position + Vector3.up * .85f + horizontalDirection * .55f;
            Vector3 targetPoint = target.transform.position + Vector3.up * .9f;
            Vector3 ray = targetPoint - origin;

            if (!Physics.Raycast(
                    origin,
                    ray.normalized,
                    out RaycastHit hit,
                    ray.magnitude,
                    lineOfSightLayers,
                    QueryTriggerInteraction.Ignore))
                return true;

            return hit.transform.root == target.transform.root;
        }

        private void CancelPendingAttack()
        {
            if (!IsAttackWindingUp)
                return;

            attackHitTime = -1f;
            SetTelegraph(false);
        }

        private void SetTelegraph(bool active)
        {
            if (botRenderer != null)
                botRenderer.material.color = active ? attackTelegraphColor : normalColor;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            attackRange = Mathf.Max(.1f, attackRange);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackKnockbackImpulse = Mathf.Max(0f, attackKnockbackImpulse);
            attackWindup = Mathf.Max(0f, attackWindup);
            attackCooldown = Mathf.Max(0f, attackCooldown);
            repathInterval = Mathf.Max(.1f, repathInterval);
            targetRepathDistance = Mathf.Max(.1f, targetRepathDistance);
            targetSampleDistance = Mathf.Max(.1f, targetSampleDistance);

            navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent != null)
                ConfigureNavigationAgent();
        }
    }
}
