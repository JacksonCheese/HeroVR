using UnityEngine;
using UnityEngine.SceneManagement;
using HeroVR.Combat;

namespace HeroVR.Prototype
{
    public class PrototypeArenaBootstrap : MonoBehaviour
    {
        private const string PrototypeSceneName = "PrototypeArena";
        private const string PropsRootName = "PrototypePhysicsProps";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeBootstrapExists()
        {
            if (SceneManager.GetActiveScene().name != PrototypeSceneName)
                return;

            if (FindObjectsByType<PrototypeArenaBootstrap>(FindObjectsInactive.Include).Length > 0)
                return;

            new GameObject(nameof(PrototypeArenaBootstrap))
                .AddComponent<PrototypeArenaBootstrap>();
        }

        private void Start()
        {
            ConfigureArena();

            DesktopHeroController player = GetOrCreatePlayer();
            DisableNonPlayerCameras(player);

            TrainingBot enemy = GetOrCreateEnemy();
            player.SetOpponent(enemy);
            enemy.SetTarget(player);

            CreatePhysicsPropsIfMissing();
        }

        private static void ConfigureArena()
        {
            ConfigureFloor();

            ConfigureOrCreateCube("NorthWall", new Vector3(0f, 2f, 13f), new Vector3(26f, 5f, 1f), Color.gray);
            ConfigureOrCreateCube("SouthWall", new Vector3(0f, 2f, -13f), new Vector3(26f, 5f, 1f), Color.gray);
            ConfigureOrCreateCube("EastWall", new Vector3(13f, 2f, 0f), new Vector3(1f, 5f, 26f), Color.gray);
            ConfigureOrCreateCube("WestWall", new Vector3(-13f, 2f, 0f), new Vector3(1f, 5f, 26f), Color.gray);
        }

        private static void ConfigureFloor()
        {
            GameObject floor = GameObject.Find("Floor");
            if (floor == null)
            {
                ConfigureOrCreateCube(
                    "Floor",
                    new Vector3(0f, -.5f, 0f),
                    new Vector3(26f, 1f, 26f),
                    new Color(.18f, .2f, .24f));
                return;
            }

            floor.transform.position = Vector3.zero;

            if (floor.TryGetComponent(out Renderer floorRenderer))
            {
                Vector3 renderedSize = floorRenderer.bounds.size;
                Vector3 scale = floor.transform.localScale;

                if (renderedSize.x > Mathf.Epsilon)
                    scale.x *= 26f / renderedSize.x;
                if (renderedSize.z > Mathf.Epsilon)
                    scale.z *= 26f / renderedSize.z;

                floor.transform.localScale = scale;
                floorRenderer.material.color = new Color(.18f, .2f, .24f);
            }
        }

        private static DesktopHeroController GetOrCreatePlayer()
        {
            DesktopHeroController existingPlayer = FindAnyObjectByType<DesktopHeroController>();
            if (existingPlayer != null)
                return existingPlayer;

            GameObject playerObject = new GameObject("PrototypeHero");
            playerObject.transform.position = new Vector3(0f, .05f, -7f);

            CharacterController controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .4f;
            controller.center = new Vector3(0f, .9f, 0f);

            playerObject.AddComponent<Damageable>();
            return playerObject.AddComponent<DesktopHeroController>();
        }

        private static void DisableNonPlayerCameras(DesktopHeroController player)
        {
            foreach (Camera sceneCamera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            {
                if (sceneCamera.transform.IsChildOf(player.transform))
                    continue;

                sceneCamera.gameObject.SetActive(false);
            }
        }

        private static TrainingBot GetOrCreateEnemy()
        {
            TrainingBot existingEnemy = FindAnyObjectByType<TrainingBot>();
            if (existingEnemy != null)
                return existingEnemy;

            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "EnemyHero";
            enemyObject.transform.position = new Vector3(0f, 1f, 6f);
            enemyObject.GetComponent<Renderer>().material.color = new Color(.85f, .16f, .18f);

            Rigidbody enemyBody = enemyObject.AddComponent<Rigidbody>();
            enemyBody.mass = 2.5f;
            enemyBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            enemyObject.AddComponent<Damageable>();
            return enemyObject.AddComponent<TrainingBot>();
        }

        private static void CreatePhysicsPropsIfMissing()
        {
            if (GameObject.Find(PropsRootName) != null)
                return;

            Transform propsRoot = new GameObject(PropsRootName).transform;
            Vector3[] propPositions =
            {
                new Vector3(-5f, .6f, 0f),
                new Vector3(5f, .6f, 0f),
                new Vector3(-4f, .6f, 3f),
                new Vector3(4f, .6f, 3f),
                new Vector3(-5f, .6f, -4f),
                new Vector3(5f, .6f, -4f)
            };

            for (int index = 0; index < propPositions.Length; index++)
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = $"PhysicsProp_{index + 1}";
                box.transform.SetParent(propsRoot);
                box.transform.position = propPositions[index];
                box.transform.localScale = Vector3.one * 1.2f;
                box.AddComponent<Rigidbody>();
            }
        }

        private static GameObject ConfigureOrCreateCube(
            string objectName,
            Vector3 position,
            Vector3 scale,
            Color color)
        {
            GameObject arenaObject = GameObject.Find(objectName);
            if (arenaObject == null)
            {
                arenaObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arenaObject.name = objectName;
            }

            arenaObject.transform.position = position;
            arenaObject.transform.localScale = scale;

            if (arenaObject.TryGetComponent(out Renderer objectRenderer))
                objectRenderer.material.color = color;

            return arenaObject;
        }
    }
}
