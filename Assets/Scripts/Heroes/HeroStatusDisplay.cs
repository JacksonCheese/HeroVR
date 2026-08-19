using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Heroes
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(Damageable),
        typeof(HeroProfile),
        typeof(HeroUltimateCharge))]
    public sealed class HeroStatusDisplay : MonoBehaviour
    {
        [SerializeField] private TextMesh statusText;
        [SerializeField] private Transform viewer;

        private Damageable health;
        private HeroProfile profile;
        private HeroUltimateCharge ultimateCharge;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            profile = GetComponent<HeroProfile>();
            ultimateCharge = GetComponent<HeroUltimateCharge>();
        }

        private void OnEnable()
        {
            health.HealthChanged += OnHealthChanged;
            ultimateCharge.ChargeChanged += OnChargeChanged;
            Refresh();
        }

        private void OnDisable()
        {
            health.HealthChanged -= OnHealthChanged;
            ultimateCharge.ChargeChanged -= OnChargeChanged;
        }

        private void LateUpdate()
        {
            if (statusText == null || viewer == null)
                return;

            Vector3 awayFromViewer = statusText.transform.position - viewer.position;
            if (awayFromViewer.sqrMagnitude > .0001f)
            {
                statusText.transform.rotation = Quaternion.LookRotation(
                    awayFromViewer.normalized,
                    viewer.up);
            }
        }

        public void Configure(TextMesh text, Transform viewingTransform)
        {
            statusText = text;
            viewer = viewingTransform;
        }

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            Refresh();
        }

        private void OnChargeChanged(float currentCharge, float maximumCharge)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (statusText == null)
                return;

            HeroDefinition definition = profile.Definition;
            string heroName = definition != null
                ? definition.DisplayName
                : "Hero";
            string ultimate = ultimateCharge.IsUltimateReady
                ? $"{(definition != null ? definition.UltimateName : "Ultimate")} READY"
                : $"{(definition != null ? definition.ResourceName : "Charge")} {Mathf.RoundToInt(ultimateCharge.NormalizedCharge * 100f)}%";

            statusText.text =
                $"{heroName}\nHP {Mathf.CeilToInt(health.CurrentHealth)}/{Mathf.CeilToInt(health.MaxHealth)}\n{ultimate}";

            if (definition != null)
                statusText.color = definition.SignatureColor;
        }
    }
}
