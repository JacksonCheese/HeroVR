using System;
using HeroVR.Heroes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroVR.Experimental
{
    /// <summary>
    /// In-game hero switching.
    ///
    /// EXPERIMENTAL / ADDITIVE. Lives outside Assets/Scripts and changes no gameplay-owned file.
    ///
    /// Heroes are not interchangeable data in this project: Thor carries LightningAbility,
    /// RecallableWeapon and XRWeaponInputAdapter, while the base player carries ProjectileCaster
    /// and WebSwingLocomotion. Because the component sets genuinely differ, swapping a
    /// HeroDefinition alone cannot switch character - the abilities themselves have to change.
    /// So this replaces the whole player object with that hero's prefab, reusing the prefabs
    /// gameplay already ships rather than inventing a parallel spawn path.
    ///
    /// The new player is placed at the outgoing player's position and rotation, and its health is
    /// left at whatever the incoming HeroDefinition specifies, so switching is a deliberate
    /// between-fights action rather than a way to refill health mid-fight.
    ///
    /// This is a playtest harness, not the shipping feature. A real hero select belongs in the
    /// match flow next to GameplayMatchBootstrap, with a proper selection UI, and should decide
    /// whether switching is allowed mid-match at all.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroSelectController : MonoBehaviour
    {
        [Serializable]
        public struct HeroOption
        {
            [Tooltip("Shown in the log when switching.")]
            public string displayName;

            [Tooltip("Full player prefab for this hero, including its ability components.")]
            public GameObject playerPrefab;
        }

        [SerializeField] private HeroOption[] heroes = Array.Empty<HeroOption>();

        [Tooltip("Button that cycles to the next hero. Defaults to left Y, which neither Thor " +
                 "nor the wall-crawler binds.")]
        [SerializeField] private string switchBinding = "<XRController>{LeftHand}/secondaryButton";

        [Tooltip("Also allow a keyboard key, so hero switching can be tested without a headset.")]
        [SerializeField] private string desktopBinding = "<Keyboard>/tab";

        [Tooltip("Ignore repeat presses for this long. Without it a single press can register " +
                 "across several frames and skip heroes.")]
        [SerializeField, Min(0f)] private float switchCooldown = .6f;

        private InputAction switchAction;
        private int currentIndex;
        private float nextAllowedSwitch;

        private void Awake()
        {
            switchAction = new InputAction("HeroSwitch", InputActionType.Button);

            if (!string.IsNullOrWhiteSpace(switchBinding))
                switchAction.AddBinding(switchBinding);
            if (!string.IsNullOrWhiteSpace(desktopBinding))
                switchAction.AddBinding(desktopBinding);
        }

        private void OnEnable()
        {
            switchAction.Enable();
        }

        private void OnDisable()
        {
            switchAction.Disable();
        }

        private void Start()
        {
            // Match the starting index to whoever the match bootstrap actually spawned, so the
            // first press advances to the next hero rather than re-spawning the current one.
            HeroProfile existing = FindExistingPlayer();
            if (existing == null)
                return;

            for (int index = 0; index < heroes.Length; index++)
            {
                GameObject prefab = heroes[index].playerPrefab;
                if (prefab == null)
                    continue;

                if (prefab.GetComponent<HeroProfile>() != null &&
                    existing.name.StartsWith(prefab.name, StringComparison.Ordinal))
                {
                    currentIndex = index;
                    break;
                }
            }
        }

        private void Update()
        {
            if (heroes.Length < 2 || !switchAction.WasPressedThisFrame())
                return;

            if (Time.time < nextAllowedSwitch)
                return;

            nextAllowedSwitch = Time.time + switchCooldown;
            SwitchTo((currentIndex + 1) % heroes.Length);
        }

        public void SwitchTo(int index)
        {
            if (index < 0 || index >= heroes.Length)
                return;

            GameObject prefab = heroes[index].playerPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("[HeroSelect] No prefab assigned for " + heroes[index].displayName);
                return;
            }

            HeroProfile existing = FindExistingPlayer();

            Vector3 position = existing != null ? existing.transform.position : transform.position;
            Quaternion rotation = existing != null ? existing.transform.rotation : transform.rotation;

            if (existing != null)
            {
                // Destroy immediately rather than at end of frame. The XR rig is on this object,
                // and leaving two XROrigins alive for a frame gives two active cameras.
                DestroyImmediate(existing.gameObject);
            }

            GameObject spawned = Instantiate(prefab, position, rotation);
            spawned.name = prefab.name;

            currentIndex = index;
            Debug.Log("[HeroSelect] Switched to " + heroes[index].displayName);
        }

        private static HeroProfile FindExistingPlayer()
        {
            // The training enemy has no HeroProfile, so this finds the player specifically.
            return FindFirstObjectByType<HeroProfile>(FindObjectsInactive.Exclude);
        }
    }
}
