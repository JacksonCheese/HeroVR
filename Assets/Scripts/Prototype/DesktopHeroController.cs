using UnityEngine;
using UnityEngine.InputSystem;
using HeroVR.Abilities;
using HeroVR.Combat;
using HeroVR.Movement;
using HeroVR.Heroes;

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

        public Damageable Health => health;
        public HeroAbilityLoadout AbilityLoadout => abilityLoadout;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            heroProfile = GetComponent<HeroProfile>();
            ultimateCharge = GetComponent<HeroUltimateCharge>();
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
            MeleePunchAbility punch = GetComponent<MeleePunchAbility>();
            if (punch == null)
            {
                punch = gameObject.AddComponent<MeleePunchAbility>();
                punch.SetCooldown(.35f);
            }

            ProjectileCaster projectile = GetComponent<ProjectileCaster>();
            if (projectile == null)
            {
                projectile = gameObject.AddComponent<ProjectileCaster>();
                projectile.SetCooldown(.55f);
            }

            DashAbility dash = GetComponent<DashAbility>();
            if (dash == null)
            {
                dash = gameObject.AddComponent<DashAbility>();
                dash.SetCooldown(1.5f);
            }

            RadialSmashAbility smash = GetComponent<RadialSmashAbility>();
            if (smash == null)
            {
                smash = gameObject.AddComponent<RadialSmashAbility>();
                smash.SetCooldown(4f);
            }

            punch.SetAttackOrigin(viewCamera.transform);
            projectile.SetSpawnPoint(projectileSpawnPoint);
            dash.SetDirectionSource(transform);
            smash.SetCenterPoint(transform);
            abilityLoadout.Configure(punch, projectile, dash, smash);
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
            GUI.Box(new Rect(18, 18, 350, 190), $"{heroName.ToUpperInvariant()} — SANDBOX");
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

            GUI.Label(new Rect(32, 120, 320, 75),
                "WASD Move | Mouse Look | Space Super Jump\n" +
                $"LMB {(definition != null ? definition.PrimaryName : "Punch")} | " +
                $"RMB {(definition != null ? definition.SecondaryName : "Blast")}\n" +
                $"E {(definition != null ? definition.UltimateName : "Ultimate")} (full charge)\n" +
                $"Shift {(definition != null ? definition.MovementName : "Dash")} | Esc Release Mouse");
            GUI.Label(
                new Rect(Screen.width / 2f - 5f, Screen.height / 2f - 12f, 20f, 25f),
                "+");
        }
    }
}
