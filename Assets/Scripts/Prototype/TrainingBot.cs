using UnityEngine;
using HeroVR.Combat;

namespace HeroVR.Prototype
{
    [RequireComponent(typeof(Rigidbody), typeof(Damageable), typeof(RespawnOnDeath))]
    public class TrainingBot : MonoBehaviour, IOpponentReceiver
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float turnSpeed = 8f;

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.75f;
        [SerializeField] private float attackDamage = 12f;
        [SerializeField] private float attackWindup = .3f;
        [SerializeField] private float attackCooldown = .9f;
        [SerializeField, Range(1f, 180f)] private float attackFacingAngle = 65f;
        [SerializeField] private LayerMask lineOfSightLayers = ~0;
        [SerializeField] private Color attackTelegraphColor = new Color(1f, .65f, .1f);

        private Rigidbody rb;
        private Damageable health;
        private Damageable target;
        private Renderer botRenderer;
        private Color normalColor;
        private float nextAttackTime;
        private float attackHitTime = -1f;

        public Damageable Health => health;
        public Damageable Target => target;
        public bool IsAttackWindingUp => attackHitTime >= 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            health = GetComponent<Damageable>();
            botRenderer = GetComponentInChildren<Renderer>();

            if (botRenderer != null)
                normalColor = botRenderer.material.color;

            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearDamping = 4f;
        }

        public void SetTarget(Damageable combatTarget)
        {
            target = combatTarget;
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

            if (to.sqrMagnitude > .01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(to.normalized),
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
                Vector3 desired = to.normalized * moveSpeed;
                rb.AddForce((desired - horizontal) * acceleration, ForceMode.Acceleration);
                return;
            }

            if (Time.time >= nextAttackTime &&
                IsFacingTarget(to) &&
                HasLineOfSight())
                BeginAttack();
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

            target.TakeDamage(new DamageInfo(
                attackDamage,
                gameObject,
                target.transform.position + Vector3.up * .9f,
                horizontalToTarget));
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
            attackWindup = Mathf.Max(0f, attackWindup);
            attackCooldown = Mathf.Max(0f, attackCooldown);
        }
    }
}
