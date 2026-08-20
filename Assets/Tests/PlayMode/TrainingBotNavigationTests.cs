using System.Collections;
using HeroVR.Combat;
using HeroVR.Prototype;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace HeroVR.Tests
{
    public sealed class TrainingBotNavigationTests
    {
        [UnityTest]
        public IEnumerator TrainingBot_WithoutNavMeshUsesSilentDirectFallback()
        {
            GameObject targetObject = new GameObject("FallbackTarget");
            targetObject.transform.position = new Vector3(0f, 50f, 6f);
            Damageable target = targetObject.AddComponent<Damageable>();

            GameObject botObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            botObject.name = "FallbackTrainingBot";
            botObject.transform.position = Vector3.up * 50f;
            botObject.SetActive(false);
            botObject.AddComponent<Rigidbody>();
            botObject.AddComponent<Damageable>();
            botObject.AddComponent<RespawnOnDeath>();
            NavMeshAgent agent = botObject.AddComponent<NavMeshAgent>();
            agent.enabled = false;
            TrainingBot bot = botObject.AddComponent<TrainingBot>();
            botObject.SetActive(true);
            bot.SetTarget(target);

            yield return new WaitForFixedUpdate();

            Assert.That(agent.enabled, Is.False);
            Assert.That(agent.isOnNavMesh, Is.False);
            Assert.That(
                Vector3.Dot(bot.CurrentSteeringDirection, Vector3.forward),
                Is.GreaterThan(.99f));

            Object.Destroy(botObject);
            Object.Destroy(targetObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrainingBot_RoutesAroundBlockedDirectPath()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "NavigationFloor";
            floor.transform.position = new Vector3(0f, -.25f, 0f);
            floor.transform.localScale = new Vector3(30f, .5f, 60f);

            GameObject westTowerLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westTowerLeg.name = "TowerLegWest";
            westTowerLeg.transform.position = new Vector3(-4.5f, 3.9f, -16f);
            westTowerLeg.transform.localScale = new Vector3(3f, 7.8f, 9f);

            GameObject eastTowerLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastTowerLeg.name = "TowerLegEast";
            eastTowerLeg.transform.position = new Vector3(4.5f, 3.9f, -16f);
            eastTowerLeg.transform.localScale = new Vector3(3f, 7.8f, 9f);

            GameObject surfaceObject = new GameObject("NavigationSurface");
            NavMeshSurface surface = surfaceObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            targetObject.name = "NavigationTarget";
            targetObject.transform.position = new Vector3(0f, 1f, 0f);
            Damageable target = targetObject.AddComponent<Damageable>();

            GameObject botObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            botObject.name = "NavigationTrainingBot";
            botObject.transform.position = new Vector3(-6f, 1f, -24f);
            Rigidbody body = botObject.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            botObject.AddComponent<Damageable>();
            botObject.AddComponent<RespawnOnDeath>();
            TrainingBot bot = botObject.AddComponent<TrainingBot>();
            bot.SetTarget(target);

            yield return new WaitForFixedUpdate();

            NavMeshAgent agent = botObject.GetComponent<NavMeshAgent>();
            Assert.That(agent, Is.Not.Null);
            Assert.That(agent.updatePosition, Is.False);
            Assert.That(agent.updateRotation, Is.False);
            Assert.That(agent.isOnNavMesh, Is.True,
                "The bot did not bind to the test NavMesh.");

            bool enteredTowerPassage = false;
            float deadline = Time.time + 10f;
            while (Time.time < deadline &&
                   Vector3.Distance(botObject.transform.position, targetObject.transform.position) > 2.5f)
            {
                Vector3 position = botObject.transform.position;
                bool isBetweenTowerFaces = position.z >= -20.5f && position.z <= -11.5f;
                bool isInsidePassage = position.x > -2.5f && position.x < 2.5f;
                enteredTowerPassage |= isBetweenTowerFaces && isInsidePassage;
                yield return new WaitForFixedUpdate();
            }

            float finalDistance = Vector3.Distance(
                botObject.transform.position,
                targetObject.transform.position);
            Assert.That(enteredTowerPassage, Is.True,
                "The bot did not route through the six-metre tower passage.");
            Assert.That(finalDistance, Is.LessThanOrEqualTo(2.5f),
                "The bot did not reach the target after routing around the obstacle.");
            Assert.That(bot.PathQueryCount, Is.LessThan(35),
                "The bot queried paths too frequently for the configured repath interval.");

            surface.RemoveData();
            Object.Destroy(botObject);
            Object.Destroy(targetObject);
            Object.Destroy(surfaceObject);
            Object.Destroy(eastTowerLeg);
            Object.Destroy(westTowerLeg);
            Object.Destroy(floor);
            yield return null;
        }
    }
}
