using System.Collections.Generic;
using HeroVR.Bosses;
using HeroVR.Combat;
using HeroVR.Destruction;
using HeroVR.Enemies;
using HeroVR.Input;
using HeroVR.Interaction;
using HeroVR.XR;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HeroVR.Tests
{
    public sealed class PhysicsDestructionAssetConfigurationTests
    {
        private const string MinionPath =
            "Assets/Prefabs/Characters/PhysicsMinion.prefab";
        private const string BossPath =
            "Assets/Prefabs/Characters/PlaceholderGiantBoss.prefab";
        private const string WallPath =
            "Assets/Prefabs/Gameplay/Physics/BreakableTestWall.prefab";
        private const string PropPath =
            "Assets/Prefabs/Gameplay/Physics/ThrowableHeavyProp.prefab";
        private const string ScenePath =
            "Assets/Scenes/Gameplay/PhysicsDestructionSandbox.unity";

        [Test]
        public void PhysicsMinion_ComposesLegacyBehaviorWithGenericPhysicsSystems()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinionPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Damageable>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GenericEnemyBrain>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<RagdollController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GrabbableCharacter>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<ImpactDamageDealer>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled,
                Is.False);
        }

        [Test]
        public void ThrowableProp_UsesGenericGrabThrowAndImpactContract()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PropPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Rigidbody>().mass, Is.GreaterThan(1f));
            Assert.That(prefab.GetComponent<GrabbableObject>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<ThrowableObject>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<ImpactDamageDealer>(), Is.Not.Null);
        }

        [Test]
        public void BreakableWall_ProvidesThreeStatesAndTraversableBrokenContract()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WallPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<StructuralDamageReceiver>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<DestructibleStructure>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("IntactState"), Is.Not.Null);
            Assert.That(prefab.transform.Find("DamagedState"), Is.Not.Null);
            Assert.That(prefab.transform.Find("BrokenState"), Is.Not.Null);
            Assert.That(
                prefab.GetComponentsInChildren<DebrisLifecycle>(true).Length,
                Is.EqualTo(2));
        }

        [Test]
        public void PlaceholderBoss_HasDefinitionControllerAndConfigurableHitRegions()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPath);
            Assert.That(prefab, Is.Not.Null);
            BossController boss = prefab.GetComponent<BossController>();
            Assert.That(boss, Is.Not.Null);
            Assert.That(boss.Definition, Is.Not.Null);
            Assert.That(boss.Definition.MaximumHealth, Is.GreaterThan(1000f));
            Assert.That(boss.Definition.PhysicalScale, Is.GreaterThan(1f));

            BossHitRegion[] regions =
                prefab.GetComponentsInChildren<BossHitRegion>(true);
            Assert.That(regions, Has.Length.EqualTo(4));
            Assert.That(
                System.Array.Find(
                    regions,
                    region => region.RegionType == BossHitRegionType.Head)
                    .DamageMultiplier,
                Is.GreaterThan(1f));
        }

        [Test]
        public void PlayerPrefabs_ExposeXrAndDesktopGrabAdapters()
        {
            GameObject xr = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Characters/XRPlayer.prefab");
            GameObject thorXr = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Characters/ThorXRPlayer.prefab");
            GameObject desktop = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Characters/DesktopPlayer.prefab");

            Assert.That(
                xr.GetComponentsInChildren<PhysicsGrabInteractor>(true),
                Has.Length.EqualTo(2));
            Assert.That(
                thorXr.GetComponentsInChildren<PhysicsGrabInteractor>(true),
                Has.Length.EqualTo(1),
                "Thor keeps the right grip reserved for Mjolnir and grabs with the left hand.");

            DesktopGrabInputAdapter desktopGrab =
                desktop.GetComponentInChildren<DesktopGrabInputAdapter>(true);
            Assert.That(desktopGrab, Is.Not.Null);
            Assert.That(
                BindingPaths(desktopGrab.GrabInputAction),
                Does.Contain("<Keyboard>/e"));
        }

        [Test]
        public void PhysicsSandbox_ContainsWallMinionBossAndSpawnContracts()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(
                    GetSceneComponents<DestructibleStructure>(scene),
                    Has.Count.EqualTo(1));
                Assert.That(
                    GetSceneComponents<GenericEnemyBrain>(scene),
                    Has.Count.EqualTo(1));
                Assert.That(
                    GetSceneComponents<MinionSpawnPoint>(scene),
                    Has.Count.EqualTo(4));
                Assert.That(
                    GetSceneComponents<BossSpawnPoint>(scene),
                    Has.Count.EqualTo(1));
                Assert.That(
                    GetSceneComponents<BossEncounterController>(scene),
                    Has.Count.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string BindingPaths(InputAction action)
        {
            if (action == null)
                return string.Empty;

            string paths = string.Empty;
            for (int index = 0; index < action.bindings.Count; index++)
                paths += action.bindings[index].path + "\n";
            return paths;
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
    }
}
