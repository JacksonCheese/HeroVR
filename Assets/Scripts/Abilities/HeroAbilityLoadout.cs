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
            return ultimateAbility != null && ultimateAbility.TryActivate();
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
    }
}
