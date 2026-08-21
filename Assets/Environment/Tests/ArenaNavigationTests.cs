using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace HeroVR.EnvironmentTests
{
    /// <summary>
    /// Verifies the arena's baked navigation, so the training bot can actually path around the
    /// geometry instead of walking into it.
    ///
    /// These guard the environment half of bot navigation. The gameplay side already has its own
    /// tests for the agent and its fallback; what those cannot catch is an arena that was never
    /// baked, which is exactly the state this arena was in - the agent silently fell back to
    /// steering straight at its target and wedged against towers.
    /// </summary>
    public sealed class ArenaNavigationTests
    {
        private const string ScenePath = "Assets/Scenes/Arenas/Arena_Graybox_01.unity";

        // TrainingEnemy's capsule radius, so sampling reflects where that bot can stand.
        private const float SampleRadius = 2f;

        [SetUp]
        public void OpenArena()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static bool TrySnap(Vector3 point, out Vector3 onMesh)
        {
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas))
            {
                onMesh = hit.position;
                return true;
            }

            onMesh = point;
            return false;
        }

        [Test]
        public void Arena_HasBakedNavMesh()
        {
            Assert.IsTrue(
                TrySnap(new Vector3(0f, 1f, 0f), out _),
                "No NavMesh at the arena centre. The arena has not been baked, so the training " +
                "bot has no surface and will fall back to steering straight into geometry.");
        }

        [Test]
        public void SpawnPoints_AreAllOnTheNavMesh()
        {
            string[] names = { "TeamA_Spawn_1", "TeamA_Spawn_2", "TeamB_Spawn_1", "TeamB_Spawn_2" };

            foreach (string name in names)
            {
                GameObject spawn = GameObject.Find(name);
                Assert.IsNotNull(spawn, name + " is missing from the arena.");

                Assert.IsTrue(
                    TrySnap(spawn.transform.position, out _),
                    name + " is not on the NavMesh, so anything spawned there cannot navigate.");
            }
        }

        /// <summary>
        /// The straight line between the team spawns runs through a tower leg, so a valid path has
        /// to detour. This is the case that previously wedged the bot.
        /// </summary>
        [Test]
        public void PathBetweenSpawns_RoutesAroundTheTowers()
        {
            Assert.IsTrue(TrySnap(GameObject.Find("TeamB_Spawn_1").transform.position, out Vector3 from),
                "TeamB_Spawn_1 is not on the NavMesh.");
            Assert.IsTrue(TrySnap(GameObject.Find("TeamA_Spawn_1").transform.position, out Vector3 to),
                "TeamA_Spawn_1 is not on the NavMesh.");

            NavMeshPath path = new NavMeshPath();
            Assert.IsTrue(
                NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path),
                "No path between the team spawns at all.");

            Assert.AreEqual(
                NavMeshPathStatus.PathComplete, path.status,
                "Path between the spawns is only partial, so the bot cannot reach the player.");

            float straightLine = Vector3.Distance(from, to);
            float pathLength = PathLength(path);

            // A path exactly as long as the straight line would mean it runs through the tower,
            // which would indicate the obstacle never carved the mesh.
            Assert.Greater(
                pathLength, straightLine + .5f,
                "Path length " + pathLength.ToString("0.##") + "m barely exceeds the straight " +
                "line " + straightLine.ToString("0.##") + "m, so it is not routing around the " +
                "towers that sit between the spawns.");
        }

        /// <summary>Obstacles must carve holes, or the bot will path straight through them.</summary>
        [Test]
        public void SolidGeometry_IsNotWalkable()
        {
            // Inside the north tower's west leg, and inside a corner pillar.
            Vector3[] insideSolids =
            {
                new Vector3(-4.5f, 1f, 16f),
                new Vector3(-10f, 1f, -10f)
            };

            foreach (Vector3 point in insideSolids)
            {
                bool onMesh = NavMesh.SamplePosition(point, out _, .35f, NavMesh.AllAreas);

                Assert.IsFalse(
                    onMesh,
                    "Found walkable NavMesh inside solid geometry at " + point +
                    ". Obstacles are not carving the mesh, so the bot will path through them.");
            }
        }

        [Test]
        public void RampsConnectGroundToWingDecks()
        {
            Assert.IsTrue(TrySnap(new Vector3(0f, .6f, 0f), out Vector3 plaza), "Plaza is not navigable.");
            Assert.IsTrue(TrySnap(new Vector3(20f, 3.8f, 0f), out Vector3 deck), "Wing deck is not navigable.");

            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(plaza, deck, NavMesh.AllAreas, path);

            Assert.AreEqual(
                NavMeshPathStatus.PathComplete, path.status,
                "No complete path from the plaza up to the wing deck. The ramps are the only " +
                "non-jumping route to tier 1, so this should connect.");
        }

        private static float PathLength(NavMeshPath path)
        {
            float total = 0f;
            for (int index = 1; index < path.corners.Length; index++)
                total += Vector3.Distance(path.corners[index - 1], path.corners[index]);

            return total;
        }
    }
}
