using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Abilities
{
    public sealed class LightningAbility : HeroAbility
    {
        [SerializeField] private MonoBehaviour aimProviderSource;
        [SerializeField] private Transform fallbackAimTransform;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField, Min(0f)] private float range = 24f;
        [SerializeField, Min(0f)] private float damage = 30f;
        [SerializeField, Min(0f)] private float knockbackImpulse = 8f;
        [SerializeField, Min(0f)] private float visualDuration = .12f;
        [SerializeField] private LayerMask hitLayers = ~0;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[24];
        private float hideVisualTime;

        public IAimProvider AimProvider => aimProviderSource as IAimProvider;
        public Vector3 LastDirection { get; private set; }
        public Vector3 LastEndPoint { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }

        public void SetAimProvider(MonoBehaviour provider)
        {
            aimProviderSource = provider is IAimProvider ? provider : null;
        }

        public void SetFallbackAimTransform(Transform aimTransform)
        {
            fallbackAimTransform = aimTransform;
        }

        public void SetLineRenderer(LineRenderer visual)
        {
            lineRenderer = visual;
            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }

        public void ConfigureCombat(
            float abilityRange,
            float damageAmount,
            float impulse,
            float effectDuration)
        {
            range = Mathf.Max(0f, abilityRange);
            damage = Mathf.Max(0f, damageAmount);
            knockbackImpulse = Mathf.Max(0f, impulse);
            visualDuration = Mathf.Max(0f, effectDuration);
        }

        protected override bool CanActivate()
        {
            return range > 0f && (AimProvider != null || fallbackAimTransform != null);
        }

        protected override bool Activate()
        {
            IAimProvider provider = AimProvider;
            Vector3 origin = provider != null
                ? provider.Origin
                : fallbackAimTransform.position;
            Vector3 direction = provider != null
                ? provider.Direction
                : fallbackAimTransform.forward;
            if (direction.sqrMagnitude <= .0001f)
                return false;

            direction.Normalize();
            LastDirection = direction;
            LastEndPoint = origin + direction * range;

            Transform ownerRoot = Owner.transform.root;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                hitBuffer,
                range,
                hitLayers,
                QueryTriggerInteraction.Ignore);

            RaycastHit closestHit = default;
            float closestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit candidate = hitBuffer[index];
                if (candidate.collider == null ||
                    candidate.collider.transform.root == ownerRoot ||
                    candidate.distance >= closestDistance)
                {
                    continue;
                }

                closestHit = candidate;
                closestDistance = candidate.distance;
            }

            if (closestHit.collider != null)
            {
                LastEndPoint = closestHit.point;
                CombatHitResolver.Apply(
                    closestHit.collider,
                    new DamageInfo(
                        damage,
                        Owner,
                        closestHit.point,
                        direction,
                        knockbackImpulse,
                        knockbackImpulse,
                        DamageType.Energy));

                Rigidbody body = closestHit.rigidbody;
                if (body != null && !body.isKinematic)
                    body.AddForce(direction * knockbackImpulse, ForceMode.Impulse);
            }

            ShowVisual(origin, LastEndPoint);
            return true;
        }

        private void Update()
        {
            if (lineRenderer != null &&
                lineRenderer.enabled &&
                Time.time >= hideVisualTime)
            {
                lineRenderer.enabled = false;
            }
        }

        private void ShowVisual(Vector3 origin, Vector3 endPoint)
        {
            if (lineRenderer == null)
                return;

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, endPoint);
            lineRenderer.enabled = true;
            hideVisualTime = Time.time + visualDuration;
        }

        protected override void CancelActiveState()
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            range = Mathf.Max(0f, range);
            damage = Mathf.Max(0f, damage);
            knockbackImpulse = Mathf.Max(0f, knockbackImpulse);
            visualDuration = Mathf.Max(0f, visualDuration);
        }
    }
}
