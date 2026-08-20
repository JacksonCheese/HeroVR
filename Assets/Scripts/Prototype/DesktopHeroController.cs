using UnityEngine;
using UnityEngine.InputSystem;
using HeroVR.Abilities;
using HeroVR.Combat;
using HeroVR.Input;
using HeroVR.Movement;
using HeroVR.Heroes;
using HeroVR.Weapons;

namespace HeroVR.Prototype
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(Damageable), typeof(RespawnOnDeath))]
    [RequireComponent(
        typeof(DesktopCharacterMotor),
        typeof(HeroAbilityLoadout),
        typeof(CharacterKnockbackReceiver))]
    public sealed class DesktopHeroController : MonoBehaviour, IOpponentReceiver
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private DesktopCharacterMotor motor;
        [SerializeField] private HeroAbilityLoadout abilityLoadout;

        private Damageable health;
        private Damageable opponentHealth;
        private HeroProfile heroProfile;
        private HeroUltimateCharge ultimateCharge;
        private DesktopWeaponInputAdapter weaponInput;
        private ThorHammerFlight hammerFlight;

        public Damageable Health => health;
        public HeroAbilityLoadout AbilityLoadout => abilityLoadout;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            heroProfile = GetComponent<HeroProfile>();
            ultimateCharge = GetComponent<HeroUltimateCharge>();
            weaponInput = GetComponent<DesktopWeaponInputAdapter>();
            hammerFlight = GetComponent<ThorHammerFlight>();
            motor = motor != null ? motor : GetComponent<DesktopCharacterMotor>();
            abilityLoadout = abilityLoadout != null
                ? abilityLoadout
                : GetComponent<HeroAbilityLoadout>();

            EnsureDesktopRig();
            EnsureAbilityComponents();
        }

        private void Start()
        {
            LockCursor();
        }

        public void ConfigureDesktopRig(
            Camera camera,
            Transform projectileSpawn,
            DesktopCharacterMotor desktopMotor,
            HeroAbilityLoadout loadout)
        {
            viewCamera = camera;
            projectileSpawnPoint = projectileSpawn;
            motor = desktopMotor;
            abilityLoadout = loadout;
        }

        public void SetOpponent(Damageable opponent)
        {
            opponentHealth = opponent;
        }

        public void SetProjectilePrefab(EnergyProjectile projectilePrefab)
        {
            if (abilityLoadout != null &&
                abilityLoadout.SecondaryAttack is ProjectileCaster projectileCaster)
            {
                projectileCaster.SetProjectilePrefab(projectilePrefab);
            }
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                motor.SetMoveInput(Vector2.zero);
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                motor.SetMoveInput(Vector2.zero);
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    LockCursor();

                return;
            }

            if (health.IsDead)
            {
                motor.SetMoveInput(Vector2.zero);
                return;
            }

            motor.AddLookDelta(Mouse.current.delta.ReadValue());
            motor.SetMoveInput(ReadMoveInput());

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                motor.RequestJump();

            if (Mouse.current.leftButton.wasPressedThisFrame)
                abilityLoadout.TryActivatePrimary();

            if (Mouse.current.rightButton.wasPressedThisFrame)
                abilityLoadout.TryActivateSecondary();

            if (Keyboard.current.leftShiftKey.wasPressedThisFrame)
            {
                abilityLoadout.TryActivateMovementAbility(
                    motor.DesiredWorldMoveDirection);
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
                abilityLoadout.TryActivateUltimate();
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed)
                input.y++;
            if (Keyboard.current.sKey.isPressed)
                input.y--;
            if (Keyboard.current.dKey.isPressed)
                input.x++;
            if (Keyboard.current.aKey.isPressed)
                input.x--;

            return Vector2.ClampMagnitude(input, 1f);
        }

        private void EnsureDesktopRig()
        {
            if (viewCamera == null)
                viewCamera = GetComponentInChildren<Camera>(true);

            if (viewCamera == null)
            {
                GameObject cameraObject = new GameObject("HeroCamera");
                cameraObject.transform.SetParent(transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                viewCamera = cameraObject.AddComponent<Camera>();
            }

            viewCamera.gameObject.tag = "MainCamera";
            if (viewCamera.GetComponent<AudioListener>() == null)
                viewCamera.gameObject.AddComponent<AudioListener>();

            if (projectileSpawnPoint == null)
            {
                GameObject spawnObject = new GameObject("ProjectileSpawn");
                projectileSpawnPoint = spawnObject.transform;
                projectileSpawnPoint.SetParent(viewCamera.transform, false);
                projectileSpawnPoint.localPosition = Vector3.forward * 1.1f;
            }

            motor.SetViewTransform(viewCamera.transform);
        }

        private void EnsureAbilityComponents()
        {
            HeroAbility primary = abilityLoadout.PrimaryAttack;
            if (primary == null)
            {
                MeleePunchAbility punch = GetComponent<MeleePunchAbility>();
                if (punch == null)
                    punch = gameObject.AddComponent<MeleePunchAbility>();
                punch.SetCooldown(.35f);
                primary = punch;
            }

            HeroAbility secondary = abilityLoadout.SecondaryAttack;
            if (secondary == null)
            {
                ProjectileCaster projectile = GetComponent<ProjectileCaster>();
                if (projectile == null)
                    projectile = gameObject.AddComponent<ProjectileCaster>();
                projectile.SetCooldown(.55f);
                secondary = projectile;
            }

            HeroAbility movement = abilityLoadout.MovementAbility;
            if (movement == null)
            {
                DashAbility dash = GetComponent<DashAbility>();
                if (dash == null)
                    dash = gameObject.AddComponent<DashAbility>();
                dash.SetCooldown(1.5f);
                movement = dash;
            }

            HeroAbility ultimate = abilityLoadout.UltimateAbility;
            if (ultimate == null)
            {
                RadialSmashAbility smash = GetComponent<RadialSmashAbility>();
                if (smash == null)
                    smash = gameObject.AddComponent<RadialSmashAbility>();
                smash.SetCooldown(4f);
                ultimate = smash;
            }

            if (primary is MeleePunchAbility configuredPunch)
                configuredPunch.SetAttackOrigin(viewCamera.transform);
            if (secondary is ProjectileCaster configuredProjectile)
                configuredProjectile.SetSpawnPoint(projectileSpawnPoint);
            if (movement is DashAbility configuredDash)
                configuredDash.SetDirectionSource(transform);
            if (ultimate is RadialSmashAbility configuredSmash)
                configuredSmash.SetCenterPoint(transform);

            abilityLoadout.Configure(primary, secondary, movement, ultimate);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnGUI()
        {
            if (health == null)
                return;

            string heroName = heroProfile != null && heroProfile.Definition != null
                ? heroProfile.Definition.DisplayName
                : "HERO VR";
            HeroDefinition definition = heroProfile != null
                ? heroProfile.Definition
                : null;
            float panelHeight = hammerFlight != null
                ? 285f
                : weaponInput != null ? 215f : 190f;
            GUI.Box(
                new Rect(18, 18, 350, panelHeight),
                $"{heroName.ToUpperInvariant()} — SANDBOX");
            GUI.Label(new Rect(32, 48, 300, 22),
                $"Health: {Mathf.CeilToInt(health.CurrentHealth)}/{Mathf.CeilToInt(health.MaxHealth)}");

            if (ultimateCharge != null)
            {
                string ultimateStatus = ultimateCharge.IsUltimateReady
                    ? "READY"
                    : $"{Mathf.RoundToInt(ultimateCharge.NormalizedCharge * 100f)}%";
                GUI.Label(new Rect(32, 70, 300, 22),
                    $"{(definition != null ? definition.ResourceName : "Charge")} / " +
                    $"{(definition != null ? definition.UltimateName : "Ultimate")}: {ultimateStatus}");
            }

            if (opponentHealth != null)
            {
                GUI.Label(new Rect(32, 92, 300, 22),
                    $"Enemy: {Mathf.CeilToInt(opponentHealth.CurrentHealth)}");
            }

            string weaponControls = weaponInput != null
                ? $"\nQ Throw {(definition != null ? definition.PrimaryName : "Weapon")} | R Recall"
                : string.Empty;
            GUI.Label(new Rect(32, 120, 320, weaponInput != null ? 100 : 75),
                "WASD Move | Mouse Look | Space Super Jump\n" +
                $"LMB {(definition != null ? definition.PrimaryName : "Punch")} | " +
                $"RMB {(definition != null ? definition.SecondaryName : "Blast")}\n" +
                $"E {(definition != null ? definition.UltimateName : "Ultimate")} (full charge)\n" +
                $"Shift {(definition != null ? definition.MovementName : "Dash")} | Esc Release Mouse" +
                weaponControls);

            if (hammerFlight != null)
            {
                RecallableWeapon weapon = hammerFlight.Weapon;
                string weaponState = weapon != null
                    ? weapon.State.ToString()
                    : "Missing";
                GUI.Label(
                    new Rect(32, 220, 320, 58),
                    "Hold F to simulate hammer spin; tap G to launch along aim\n" +
                    $"Hammer {weaponState} | Spin {hammerFlight.SpinMagnitude:F1} rad/s | " +
                    $"Flight {(hammerFlight.IsHovering ? "HOVER" : hammerFlight.IsFlightActive ? "MOMENTUM" : "OFF")}\n" +
                    $"Gravity {hammerFlight.CurrentGravityScale:F2} | Vertical {motor.Velocity.y:F1} m/s");
            }
            GUI.Label(
                new Rect(Screen.width / 2f - 5f, Screen.height / 2f - 12f, 20f, 25f),
                "+");
        }
    }
}
