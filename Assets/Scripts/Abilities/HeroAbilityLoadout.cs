using UnityEngine;

namespace HeroVR.Abilities
{
    [DisallowMultipleComponent]
    public sealed class HeroAbilityLoadout : MonoBehaviour
    {
        [SerializeField] private HeroAbility primaryAttack;
        [SerializeField] private HeroAbility secondaryAttack;
        [SerializeField] private HeroAbility movementAbility;
        [SerializeField] private HeroAbility ultimateAbility;

        private IUltimateResource ultimateResource;

        public HeroAbility PrimaryAttack => primaryAttack;
        public HeroAbility SecondaryAttack => secondaryAttack;
        public HeroAbility MovementAbility => movementAbility;
        public HeroAbility UltimateAbility => ultimateAbility;

        public bool TryActivatePrimary()
        {
            return primaryAttack != null && primaryAttack.TryActivate();
        }

        public bool TryActivateSecondary()
        {
            return secondaryAttack != null && secondaryAttack.TryActivate();
        }

        public bool TryActivateMovementAbility()
        {
            return movementAbility != null && movementAbility.TryActivate();
        }

        public bool TryActivateMovementAbility(Vector3 worldDirection)
        {
            if (movementAbility is IDirectionalAbility directionalAbility)
                directionalAbility.SetDirection(worldDirection);

            return TryActivateMovementAbility();
        }

        public bool TryActivateUltimate()
        {
            IUltimateResource resource = GetUltimateResource();
            if (resource != null && !resource.IsUltimateReady)
                return false;

            if (ultimateAbility == null || !ultimateAbility.TryActivate())
                return false;

            return resource == null || resource.TryConsumeUltimate();
        }

        public void Configure(
            HeroAbility primary,
            HeroAbility secondary,
            HeroAbility movement,
            HeroAbility ultimate)
        {
            primaryAttack = primary;
            secondaryAttack = secondary;
            movementAbility = movement;
            ultimateAbility = ultimate;
        }

        private IUltimateResource GetUltimateResource()
        {
            if (ultimateResource != null)
                return ultimateResource;

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IUltimateResource resource)
                {
                    ultimateResource = resource;
                    break;
                }
            }

            return ultimateResource;
        }
    }
}
