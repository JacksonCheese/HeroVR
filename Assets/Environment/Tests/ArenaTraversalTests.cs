using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HeroVR.EnvironmentTests
{
    /// <summary>
    /// Verifies that Arena_Graybox_01 is traversable using DesktopPlayer's movement
    /// values. Probes the real scene colliders with raycasts rather than trusting the numbers in
    /// the builder, because both traversal bugs found so far looked correct on paper.
    /// </summary>
    public sealed class ArenaTraversalTests
    {
        private const string ScenePath = "Assets/Scenes/Arenas/Arena_Graybox_01.unity";

        // DesktopPlayer.prefab CharacterController + DesktopCharacterMotor.
        private const float StepOffset = .3f;
        private const float JumpHeight = 2.6f;
        private const float Gravity = -22f;
        private const float MoveSpeed = 7f;

        [SetUp]
        public void OpenArena()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        /// <summary>Height of the first solid surface under a point, or NaN if nothing is hit.</summary>
        private static float SurfaceHeight(float x, float z, float fromHeight = 40f)
        {
            return Physics.Raycast(new Vector3(x, fromHeight, z), Vector3.down, out RaycastHit hit, 200f)
                ? hit.point.y
                : float.NaN;
        }

        [Test]
        public void Arena_HasFourSpawnTransformsFacingCentre()
        {
            string[] names = { "TeamA_Spawn_1", "TeamA_Spawn_2", "TeamB_Spawn_1", "TeamB_Spawn_2" };

            foreach (string name in names)
            {
                GameObject spawn = GameObject.Find(name);
                Assert.IsNotNull(spawn, name + " is missing from the arena.");

                Vector3 toCentre = -spawn.transform.position;
                toCentre.y = 0f;
                float angle = Vector3.Angle(spawn.transform.forward, toCentre.normalized);
                Assert.Less(angle, 60f, name + " faces away from the arena centre.");
            }
        }

        [Test]
        public void SpawnPoints_StandOnSolidGround()
        {
            string[] names = { "TeamA_Spawn_1", "TeamA_Spawn_2", "TeamB_Spawn_1", "TeamB_Spawn_2" };

            foreach (string name in names)
            {
                Vector3 position = GameObject.Find(name).transform.position;
                float ground = SurfaceHeight(position.x, position.z);

                Assert.IsFalse(float.IsNaN(ground), name + " has no floor beneath it.");
                Assert.Less(
                    Mathf.Abs(ground - position.y), 1f,
                    name + " is not resting near the floor (floor y=" + ground + ").");
            }
        }

        /// <summary>
        /// Regression test for the first traversal bug: plaza risers taller than the step offset
        /// sealed off the central fight zone.
        /// </summary>
        [Test]
        public void PlazaSteps_AreWithinPlayerStepOffset()
        {
            // Sample outward from the centre across both risers.
            float centre = SurfaceHeight(0f, 0f);      // upper step
            float middle = SurfaceHeight(0f, 9f);      // lower step
            float outside = SurfaceHeight(0f, 12.5f);  // open ground

            Assert.IsFalse(float.IsNaN(centre) || float.IsNaN(middle) || float.IsNaN(outside),
                "Missing floor while sampling the plaza.");

            float lowerRise = middle - outside;
            float upperRise = centre - middle;

            Assert.LessOrEqual(lowerRise, StepOffset,
                "Ground -> lower plaza step rises " + lowerRise + "m, above the " + StepOffset +
                "m step offset. Nothing can walk onto the plaza.");
            Assert.LessOrEqual(upperRise, StepOffset,
                "Lower -> upper plaza step rises " + upperRise + "m, above the " + StepOffset + "m step offset.");
        }

        /// <summary>
        /// Regression test for the second traversal bug: tower hops taller than the jump height
        /// made the roofs unreachable. Checks the real surfaces and the actual ballistic arc.
        /// </summary>
        [Test]
        public void TowerClimb_IsReachableWithinJumpHeight()
        {
            float deck = SurfaceHeight(14.5f, 14.5f);  // corner platform
            float ledge = SurfaceHeight(7.5f, 16f);    // tower side ledge
            float roof = SurfaceHeight(0f, 16f);       // tower roof

            Assert.IsFalse(float.IsNaN(deck) || float.IsNaN(ledge) || float.IsNaN(roof),
                "Missing surface while sampling the tower climb.");

            float toLedge = ledge - deck;
            float toRoof = roof - ledge;

            Assert.LessOrEqual(toLedge, JumpHeight,
                "Corner platform -> tower ledge rises " + toLedge + "m, above the " + JumpHeight + "m jump.");
            Assert.LessOrEqual(toRoof, JumpHeight,
                "Tower ledge -> roof rises " + toRoof + "m, above the " + JumpHeight +
                "m jump. The arena's high ground is unreachable.");

            // The ledge is also 2.5m away horizontally, so confirm the jump arc is still high
            // enough by the time the player has covered that distance.
            const float horizontalGap = 2.5f;
            float launchSpeed = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            float timeToCross = horizontalGap / MoveSpeed;
            float heightAtGap = launchSpeed * timeToCross + .5f * Gravity * timeToCross * timeToCross;

            Assert.Greater(heightAtGap, toLedge,
                "After clearing the " + horizontalGap + "m gap the player is only " + heightAtGap +
                "m up, short of the " + toLedge + "m ledge.");
        }

        [Test]
        public void Arena_HasNoHolesAcrossThePlayfield()
        {
            // Sweep the playable area; every column should hit something.
            for (float x = -26f; x <= 26f; x += 4f)
            {
                for (float z = -26f; z <= 26f; z += 4f)
                {
                    Assert.IsFalse(
                        float.IsNaN(SurfaceHeight(x, z)),
                        "No floor at (" + x + ", " + z + ").");
                }
            }
        }
    }
}
