using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace HeroVR.EnvironmentTests
{
    /// <summary>
    /// Guards the destruction and boss environment contract.
    ///
    /// The claim worth testing is that a broken wall genuinely opens a passage. It is easy to
    /// author a broken-looking visual and leave a collider spanning the gap, which looks correct
    /// in the editor and silently blocks every thrown enemy at runtime. These tests physically
    /// sweep the opening rather than inspecting the hierarchy.
    /// </summary>
    public sealed class DestructionEnvironmentTests
    {
        private const string BreakableFolder = "Assets/Prefabs/Environment/Breakable";
        private const string PropFolder = "Assets/Prefabs/Environment/Props";
        private const string BossScene = "Assets/Scenes/Arenas/BossArena_Graybox_01.unity";
        private const string TestScene = "Assets/Scenes/Arenas/Arena_DestructionTest.unity";

        private static readonly string[] WallPrefabs =
        {
            "Wall_Breakable_Concrete",
            "Wall_Breakable_Brick",
            "Wall_Breakable_Interior"
        };

        private static readonly string[] PropPrefabs =
        {
            "Prop_Light_SmallCrate",
            "Prop_Light_TrashCan",
            "Prop_Medium_Barrel",
            "Prop_Medium_Bench",
            "Prop_Heavy_ConcreteChunk",
            "Prop_Heavy_Car"
        };

        private GameObject spawned;

        [TearDown]
        public void Cleanup()
        {
            if (spawned != null)
                Object.DestroyImmediate(spawned);
        }

        private GameObject Spawn(string folder, string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                folder + "/" + prefabName + ".prefab");
            Assert.IsNotNull(prefab, "Missing prefab " + prefabName);

            spawned = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            return spawned;
        }

        [Test]
        public void BreakableWalls_HaveAllThreeStatesWithOnlyIntactActive()
        {
            foreach (string wallName in WallPrefabs)
            {
                GameObject wall = Spawn(BreakableFolder, wallName);

                Transform intact = wall.transform.Find("IntactState");
                Transform damaged = wall.transform.Find("DamagedState");
                Transform broken = wall.transform.Find("BrokenState");

                Assert.IsNotNull(intact, wallName + " has no IntactState.");
                Assert.IsNotNull(damaged, wallName + " has no DamagedState.");
                Assert.IsNotNull(broken, wallName + " has no BrokenState.");

                Assert.IsTrue(intact.gameObject.activeSelf, wallName + " does not start intact.");
                Assert.IsFalse(damaged.gameObject.activeSelf,
                    wallName + " starts with DamagedState active; states would overlap.");
                Assert.IsFalse(broken.gameObject.activeSelf,
                    wallName + " starts with BrokenState active; states would overlap.");

                Assert.IsNotNull(broken.Find("Debris"), wallName + " broken state has no Debris.");

                Cleanup();
            }
        }

        /// <summary>
        /// The core promise of the module. Enables only the broken state and sweeps a capsule the
        /// size of a player through where the opening should be.
        /// </summary>
        [Test]
        public void BrokenState_LeavesAPhysicallyTraversableOpening()
        {
            foreach (string wallName in WallPrefabs)
            {
                GameObject wall = Spawn(BreakableFolder, wallName);
                wall.transform.Find("IntactState").gameObject.SetActive(false);
                wall.transform.Find("DamagedState").gameObject.SetActive(false);
                wall.transform.Find("BrokenState").gameObject.SetActive(true);

                Physics.SyncTransforms();

                // Player-sized capsule pushed through the wall plane at opening height.
                const float radius = .35f;
                Vector3 start = new Vector3(0f, 1.2f, -3f);
                Vector3 end = new Vector3(0f, 1.2f, 3f);

                bool blocked = Physics.CapsuleCast(
                    start + Vector3.up * .5f,
                    start - Vector3.up * .5f,
                    radius,
                    Vector3.forward,
                    out RaycastHit hit,
                    6f,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                Assert.IsFalse(
                    blocked,
                    wallName + " broken state still blocks the opening (hit " +
                    (blocked ? hit.collider.name : "none") +
                    "). A broken wall that cannot be walked through defeats the module.");

                Cleanup();
            }
        }

        /// <summary>Intact must actually block, or "breaking" it means nothing.</summary>
        [Test]
        public void IntactState_BlocksThePassage()
        {
            foreach (string wallName in WallPrefabs)
            {
                GameObject wall = Spawn(BreakableFolder, wallName);
                Physics.SyncTransforms();

                bool blocked = Physics.Raycast(
                    new Vector3(0f, 1.2f, -3f), Vector3.forward, 6f, ~0,
                    QueryTriggerInteraction.Ignore);

                Assert.IsTrue(blocked, wallName + " does not block while intact.");
                Cleanup();
            }
        }

        [Test]
        public void BreakableWalls_UseOnlySimpleColliders()
        {
            foreach (string wallName in WallPrefabs)
            {
                GameObject wall = Spawn(BreakableFolder, wallName);

                Assert.IsEmpty(
                    wall.GetComponentsInChildren<MeshCollider>(true),
                    wallName + " uses a MeshCollider. High-speed thrown objects tunnel through " +
                    "and ragdolls snag on them; primitives only.");

                Cleanup();
            }
        }

        [Test]
        public void Props_ArePhysicsReadyWithoutGameplayComponents()
        {
            foreach (string propName in PropPrefabs)
            {
                GameObject prop = Spawn(PropFolder, propName);

                Assert.IsNotEmpty(
                    prop.GetComponentsInChildren<Collider>(true),
                    propName + " has no collider, so it cannot be grabbed or thrown.");

                Assert.IsEmpty(
                    prop.GetComponentsInChildren<MeshCollider>(true),
                    propName + " uses a MeshCollider; primitives only for thrown props.");

                Assert.IsEmpty(
                    prop.GetComponentsInChildren<Rigidbody>(true),
                    propName + " ships a Rigidbody. Mass and physics are gameplay-owned; the " +
                    "environment should not pre-empt them.");

                Assert.IsNotEmpty(
                    prop.GetComponentsInChildren<Renderer>(true),
                    propName + " has no renderer.");

                Cleanup();
            }
        }

        [Test]
        public void BossArena_HasSpawnHooksAndClearBossZone()
        {
            EditorSceneManager.OpenScene(BossScene, OpenSceneMode.Single);

            Assert.IsNotNull(GameObject.Find("BossSpawnPoint"), "No BossSpawnPoint in the boss arena.");
            Assert.IsNotNull(GameObject.Find("ArenaCenter"), "No ArenaCenter in the boss arena.");

            string[] minions = { "A1", "A2", "B1", "B2", "C1", "C2" };
            foreach (string label in minions)
            {
                Assert.IsNotNull(
                    GameObject.Find("MinionSpawn_" + label),
                    "Missing MinionSpawn_" + label + ".");
            }

            // The boss needs unobstructed floor. Sweep the middle of the zone at boss chest
            // height; anything solid here would have a giant enemy clipping it constantly.
            for (float angle = 0f; angle < 360f; angle += 45f)
            {
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                bool obstructed = Physics.Raycast(
                    new Vector3(0f, 5f, 0f), direction, 18f, ~0, QueryTriggerInteraction.Ignore);

                Assert.IsFalse(
                    obstructed,
                    "Boss zone is obstructed at " + angle + " degrees. The central floor must " +
                    "stay clear or a 3x-6x scale boss clips geometry on every move.");
            }
        }

        [Test]
        public void BossArena_IsNavigableForMinions()
        {
            EditorSceneManager.OpenScene(BossScene, OpenSceneMode.Single);

            GameObject boss = GameObject.Find("BossSpawnPoint");
            Assert.IsTrue(
                NavMesh.SamplePosition(boss.transform.position, out NavMeshHit bossHit, 3f, NavMesh.AllAreas),
                "BossSpawnPoint is not on the NavMesh.");

            string[] minions = { "A1", "B1", "C1" };
            foreach (string label in minions)
            {
                GameObject spawn = GameObject.Find("MinionSpawn_" + label);
                Assert.IsTrue(
                    NavMesh.SamplePosition(spawn.transform.position, out NavMeshHit spawnHit, 3f, NavMesh.AllAreas),
                    "MinionSpawn_" + label + " is not on the NavMesh, so minions cannot path from it.");

                NavMeshPath path = new NavMeshPath();
                NavMesh.CalculatePath(spawnHit.position, bossHit.position, NavMesh.AllAreas, path);

                Assert.AreEqual(
                    NavMeshPathStatus.PathComplete, path.status,
                    "Minions cannot reach the boss zone from MinionSpawn_" + label + ".");
            }
        }

        [Test]
        public void DestructionTestArea_HasBreakablesAndProps()
        {
            EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Single);

            Assert.IsNotNull(GameObject.Find("Exterior_Concrete"), "No breakable exterior wall.");
            Assert.IsNotNull(GameObject.Find("Interior_Divider"), "No breakable interior wall.");
            Assert.IsNotNull(GameObject.Find("PlayerStart"), "No PlayerStart hook.");

            int props = 0;
            foreach (string propName in PropPrefabs)
            {
                if (GameObject.Find(propName) != null)
                    props++;
            }

            Assert.GreaterOrEqual(props, 4, "Destruction test area has too few throwable props.");
        }
    }
}
