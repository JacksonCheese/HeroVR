using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HeroVR.Abilities;
using HeroVR.Arena;
using HeroVR.Gameplay;
using HeroVR.Heroes;
using HeroVR.Input;
using HeroVR.Prototype;
using HeroVR.Weapons;
using HeroVR.XR;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;

namespace HeroVR.Tests
{
    public sealed class GameplayAssetConfigurationTests
    {
        private const string XrPlayerPath =
            "Assets/Prefabs/Characters/XRPlayer.prefab";
        private const string ThorPlayerPath =
            "Assets/Prefabs/Characters/ThorXRPlayer.prefab";
        private const string ThorDesktopPlayerPath =
            "Assets/Prefabs/Characters/ThorDesktopPlayer.prefab";
        private const string TrainingEnemyPath =
            "Assets/Prefabs/Characters/TrainingEnemy.prefab";
        private const string ThorArenaPath =
            "Assets/Scenes/Arenas/Arena_ThorVRTest.unity";
        private const string ThorDesktopArenaPath =
            "Assets/Scenes/Arenas/Arena_ThorDesktopTest.unity";
        private const string ThorFlightSettingsPath =
            "Assets/Heroes/Thor/ThorHammerFlightSettings.asset";

        [Test]
        public void XrPlayer_UsesRightPrimaryJumpAndPointerAimPose()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrPlayerPath);
            Assert.That(prefab, Is.Not.Null);

            XRHeroInputAdapter input = prefab.GetComponent<XRHeroInputAdapter>();
            Assert.That(input, Is.Not.Null);
            Assert.That(
                BindingPaths(input.JumpInputAction),
                Does.Contain("<XRController>{RightHand}/{primaryButton}"));
            Assert.That(
                BindingPaths(input.JumpInputAction),
                Does.Not.Contain("<XRController>{LeftHand}/{primaryButton}"));
            Assert.That(
                BindingPaths(input.UltimateInputAction),
                Does.Not.Contain("<XRController>{RightHand}/{primaryButton}"));

            TransformAimProvider aim =
                prefab.GetComponentInChildren<TransformAimProvider>(true);
            Assert.That(aim, Is.Not.Null);
            TrackedPoseDriver pose =
                aim.transform.parent.GetComponent<TrackedPoseDriver>();
            Assert.That(pose, Is.Not.Null);
            Assert.That(
                BindingPaths(pose.positionInput.action),
                Does.Contain("<XRController>{RightHand}/pointerPosition"));
            Assert.That(
                BindingPaths(pose.rotationInput.action),
                Does.Contain("<XRController>{RightHand}/pointerRotation"));
        }

        [Test]
        public void ThorPrefab_ComposesReusableHeroWeaponAndLightningSystems()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThorPlayerPath);
            Assert.That(prefab, Is.Not.Null);

            HeroProfile profile = prefab.GetComponent<HeroProfile>();
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.Definition, Is.Not.Null);
            Assert.That(profile.Definition.HeroId, Is.EqualTo("thor"));
            Assert.That(prefab.GetComponent<LightningAbility>(), Is.Not.Null);

            RecallableWeapon weapon =
                prefab.GetComponentInChildren<RecallableWeapon>(true);
            XRWeaponInputAdapter weaponInput =
                prefab.GetComponent<XRWeaponInputAdapter>();
            TransformWeaponMotionSource motionSource =
                prefab.GetComponent<TransformWeaponMotionSource>();
            ThorHammerFlight flight = prefab.GetComponent<ThorHammerFlight>();
            Assert.That(weapon, Is.Not.Null);
            Assert.That(weaponInput, Is.Not.Null);
            Assert.That(weaponInput.Weapon, Is.SameAs(weapon));
            Assert.That(motionSource, Is.Not.Null);
            Assert.That(flight, Is.Not.Null);
            Assert.That(flight.Weapon, Is.SameAs(weapon));
            Assert.That(flight.Settings, Is.Not.Null);
            Assert.That(
                BindingPaths(weaponInput.GripInputAction),
                Does.Contain("<XRController>{RightHand}/{gripButton}"));
            Assert.That(
                BindingPaths(weaponInput.RecallInputAction),
                Does.Contain("<XRController>{RightHand}/{secondaryButton}"));
        }

        [Test]
        public void ThorDesktopPrefab_ReusesThorLoadoutWithDesktopAdapters()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ThorDesktopPlayerPath);
            Assert.That(prefab, Is.Not.Null);

            HeroProfile profile = prefab.GetComponent<HeroProfile>();
            HeroAbilityLoadout loadout = prefab.GetComponent<HeroAbilityLoadout>();
            DesktopWeaponInputAdapter weaponInput =
                prefab.GetComponent<DesktopWeaponInputAdapter>();
            DesktopThorFlightDebugAdapter flightDebug =
                prefab.GetComponent<DesktopThorFlightDebugAdapter>();
            ThorHammerFlight flight = prefab.GetComponent<ThorHammerFlight>();
            RecallableWeapon weapon =
                prefab.GetComponentInChildren<RecallableWeapon>(true);

            Assert.That(profile.Definition.HeroId, Is.EqualTo("thor"));
            Assert.That(prefab.GetComponent<DesktopHeroController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<XRHeroInputAdapter>(), Is.Null);
            Assert.That(loadout.SecondaryAttack, Is.TypeOf<LightningAbility>());
            Assert.That(prefab.GetComponent<ProjectileCaster>(), Is.Null);
            Assert.That(weaponInput, Is.Not.Null);
            Assert.That(weaponInput.Weapon, Is.SameAs(weapon));
            Assert.That(flightDebug, Is.Not.Null);
            Assert.That(flight, Is.Not.Null);
            Assert.That(flight.Weapon, Is.SameAs(weapon));
            Assert.That(
                BindingPaths(weaponInput.ThrowInputAction),
                Does.Contain("<Keyboard>/q"));
            Assert.That(
                BindingPaths(weaponInput.RecallInputAction),
                Does.Contain("<Keyboard>/r"));
            Assert.That(
                BindingPaths(flightDebug.SpinInputAction),
                Does.Contain("<Keyboard>/f"));
            Assert.That(
                BindingPaths(flightDebug.LaunchInputAction),
                Does.Contain("<Keyboard>/g"));
        }

        [Test]
        public void ThorFlightSettings_ExposeStableLaunchAndHoverHysteresis()
        {
            ThorHammerFlightSettings settings =
                AssetDatabase.LoadAssetAtPath<ThorHammerFlightSettings>(
                    ThorFlightSettingsPath);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.MinimumSpinSpeed, Is.GreaterThan(0f));
            Assert.That(settings.RequiredSpinDuration, Is.GreaterThan(0f));
            Assert.That(settings.LaunchMotionThreshold, Is.GreaterThan(0f));
            Assert.That(settings.LaunchImpulse, Is.GreaterThan(0f));
            Assert.That(settings.MaximumFlightSpeed, Is.GreaterThan(0f));
            Assert.That(
                settings.HoverActivationSpinSpeed,
                Is.GreaterThan(settings.HoverDeactivationSpinSpeed));
            Assert.That(settings.HoverGravityMultiplier, Is.InRange(0f, 1f));
        }

        [Test]
        public void TrainingEnemy_PrefabAgentStartsDisabledForUnbakedScenes()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TrainingEnemyPath);
            Assert.That(prefab, Is.Not.Null);

            NavMeshAgent agent = prefab.GetComponent<NavMeshAgent>();
            Assert.That(agent, Is.Not.Null);
            Assert.That(agent.enabled, Is.False);
        }

        [Test]
        public void ThorArena_UsesGenericSpawnContractsAndGameplayBootstrap()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ThorArenaPath,
                OpenSceneMode.Additive);

            try
            {
                List<ArenaSpawnPoint> spawnPoints =
                    GetSceneComponents<ArenaSpawnPoint>(scene);
                Assert.That(spawnPoints, Has.Count.EqualTo(4));
                Assert.That(
                    spawnPoints.FindAll(
                        point => point.SpawnType == ArenaSpawnType.Player),
                    Has.Count.EqualTo(2));
                Assert.That(
                    spawnPoints.FindAll(
                        point => point.SpawnType == ArenaSpawnType.TrainingEnemy),
                    Has.Count.EqualTo(2));
                Assert.That(
                    spawnPoints.FindAll(point => point.Team == ArenaTeam.TeamOne),
                    Has.Count.EqualTo(2));
                Assert.That(
                    spawnPoints.FindAll(point => point.Team == ArenaTeam.TeamTwo),
                    Has.Count.EqualTo(2));
                Assert.That(
                    GetSceneComponents<GameplayMatchBootstrap>(scene),
                    Has.Count.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void ThorDesktopArena_SpawnsDesktopThorThroughGenericBootstrap()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ThorDesktopArenaPath,
                OpenSceneMode.Additive);

            try
            {
                List<ArenaSpawnPoint> spawnPoints =
                    GetSceneComponents<ArenaSpawnPoint>(scene);
                List<GameplayMatchBootstrap> bootstraps =
                    GetSceneComponents<GameplayMatchBootstrap>(scene);

                Assert.That(spawnPoints, Has.Count.EqualTo(4));
                Assert.That(bootstraps, Has.Count.EqualTo(1));
                Assert.That(bootstraps[0].PlayerPrefab, Is.Not.Null);
                Assert.That(
                    bootstraps[0].PlayerPrefab.GetComponent<DesktopHeroController>(),
                    Is.Not.Null);
                Assert.That(
                    bootstraps[0].PlayerPrefab.GetComponent<HeroProfile>()
                        .Definition.HeroId,
                    Is.EqualTo("thor"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void OpenXr_PreservesPcTouchAndEnablesQuestTouchSupport()
        {
            Assert.That(
                HasSerializedEnabledFeature(
                    "OculusTouchControllerProfile Android"),
                Is.True);
            Assert.That(
                HasSerializedEnabledFeature("MetaQuestFeature Android"),
                Is.True);
            Assert.That(
                HasSerializedEnabledFeature(
                    "OculusTouchControllerProfile Standalone"),
                Is.True);
        }

        private static bool HasSerializedEnabledFeature(string featureName)
        {
            string path = Path.Combine(
                Application.dataPath,
                "XR/Settings/OpenXRPackageSettings.asset");
            string contents = File.ReadAllText(path);
            string pattern =
                $"m_Name: {Regex.Escape(featureName)}\\r?\\n" +
                "  m_EditorClassIdentifier: [^\\r\\n]+\\r?\\n" +
                "  m_enabled: 1";
            return Regex.IsMatch(contents, pattern);
        }

        private static List<TComponent> GetSceneComponents<TComponent>(Scene scene)
            where TComponent : Component
        {
            List<TComponent> components = new List<TComponent>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                components.AddRange(
                    roots[index].GetComponentsInChildren<TComponent>(true));
            }

            return components;
        }

        private static List<string> BindingPaths(InputAction action)
        {
            Assert.That(action, Is.Not.Null);
            List<string> paths = new List<string>(action.bindings.Count);
            for (int index = 0; index < action.bindings.Count; index++)
                paths.Add(action.bindings[index].path);
            return paths;
        }
    }
}
