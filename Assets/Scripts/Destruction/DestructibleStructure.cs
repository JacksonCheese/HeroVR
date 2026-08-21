using System;
using UnityEngine;

namespace HeroVR.Destruction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StructuralDamageReceiver))]
    public sealed class DestructibleStructure : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float damagedHealthFraction = .6f;
        [SerializeField] private GameObject intactState;
        [SerializeField] private GameObject damagedState;
        [SerializeField] private GameObject brokenState;
        [SerializeField] private Collider[] blockingColliders;
        [SerializeField] private DebrisLifecycle[] debris;

        private StructuralDamageReceiver receiver;

        public StructureState State { get; private set; } = StructureState.Intact;
        public StructuralDamageReceiver Receiver => receiver;

        public event Action<StructureState> StateChanged;
        public event Action Broken;

        private void Awake()
        {
            receiver = GetComponent<StructuralDamageReceiver>();
            ApplyState(StructureState.Intact, false);
        }

        private void OnEnable()
        {
            receiver.HealthChanged += OnHealthChanged;
            receiver.Broken += OnBroken;
        }

        private void OnDisable()
        {
            receiver.HealthChanged -= OnHealthChanged;
            receiver.Broken -= OnBroken;
        }

        public void Configure(
            float damagedThreshold,
            GameObject intactVisual,
            GameObject damagedVisual,
            GameObject brokenVisual,
            Collider[] solidColliders,
            DebrisLifecycle[] debrisObjects = null)
        {
            damagedHealthFraction = Mathf.Clamp01(damagedThreshold);
            intactState = intactVisual;
            damagedState = damagedVisual;
            brokenState = brokenVisual;
            blockingColliders = solidColliders ?? Array.Empty<Collider>();
            debris = debrisObjects ?? Array.Empty<DebrisLifecycle>();
        }

        public void ResetStructure()
        {
            receiver.ResetStructure();
            for (int index = 0; index < debris.Length; index++)
            {
                if (debris[index] != null)
                    debris[index].ResetDebris();
            }

            ApplyState(StructureState.Intact, true);
        }

        private void OnHealthChanged(float current, float maximum)
        {
            if (receiver.IsBroken)
                return;

            StructureState nextState = maximum > 0f &&
                current / maximum <= damagedHealthFraction
                ? StructureState.Damaged
                : StructureState.Intact;
            ApplyState(nextState, true);
        }

        private void OnBroken()
        {
            if (State == StructureState.Broken)
                return;

            ApplyState(StructureState.Broken, true);
            for (int index = 0; index < debris.Length; index++)
            {
                if (debris[index] != null)
                    debris[index].Activate();
            }
            Broken?.Invoke();
        }

        private void ApplyState(StructureState state, bool notify)
        {
            bool changed = State != state;
            State = state;
            if (intactState != null)
                intactState.SetActive(state == StructureState.Intact);
            if (damagedState != null)
                damagedState.SetActive(state == StructureState.Damaged);
            if (brokenState != null)
                brokenState.SetActive(state == StructureState.Broken);

            bool blocksPassage = state != StructureState.Broken;
            for (int index = 0; index < blockingColliders.Length; index++)
            {
                if (blockingColliders[index] != null)
                    blockingColliders[index].enabled = blocksPassage;
            }

            if (changed && notify)
                StateChanged?.Invoke(state);
        }

        private void OnValidate()
        {
            damagedHealthFraction = Mathf.Clamp01(damagedHealthFraction);
        }
    }
}
