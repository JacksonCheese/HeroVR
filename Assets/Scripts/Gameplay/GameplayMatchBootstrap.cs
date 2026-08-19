using UnityEngine;
using HeroVR.Arena;
using HeroVR.Combat;

namespace HeroVR.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayMatchBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject desktopPlayerPrefab;
        [SerializeField] private GameObject trainingEnemyPrefab;
        [SerializeField] private bool startAutomatically = true;

        public GameObject SpawnedPlayer { get; private set; }
        public GameObject SpawnedEnemy { get; private set; }
        public bool HasStarted { get; private set; }

        private void Start()
        {
            if (startAutomatically)
                TryStartMatch();
        }

        public void Configure(
            GameObject playerPrefab,
            GameObject enemyPrefab,
            bool autoStart = true)
        {
            desktopPlayerPrefab = playerPrefab;
            trainingEnemyPrefab = enemyPrefab;
            startAutomatically = autoStart;
        }

        public bool TryStartMatch()
        {
            if (HasStarted)
                return false;

            if (desktopPlayerPrefab == null || trainingEnemyPrefab == null)
            {
                Debug.LogError("Gameplay match requires player and enemy prefabs.", this);
                return false;
            }

            ArenaSpawnPoint playerSpawn = null;
            ArenaSpawnPoint enemySpawn = null;
            ArenaSpawnPoint[] spawnPoints = FindObjectsByType<ArenaSpawnPoint>(
                FindObjectsInactive.Exclude);

            for (int index = 0; index < spawnPoints.Length; index++)
            {
                ArenaSpawnPoint spawnPoint = spawnPoints[index];
                if (spawnPoint.SpawnType == ArenaSpawnType.Player && playerSpawn == null)
                    playerSpawn = spawnPoint;
                else if (spawnPoint.SpawnType == ArenaSpawnType.TrainingEnemy && enemySpawn == null)
                    enemySpawn = spawnPoint;
            }

            if (playerSpawn == null || enemySpawn == null)
            {
                Debug.LogError(
                    "Gameplay match requires one Player and one TrainingEnemy ArenaSpawnPoint.",
                    this);
                return false;
            }

            SpawnedPlayer = InstantiateAt(desktopPlayerPrefab, playerSpawn.transform);
            SpawnedEnemy = InstantiateAt(trainingEnemyPrefab, enemySpawn.transform);

            Damageable playerHealth = SpawnedPlayer.GetComponentInChildren<Damageable>();
            Damageable enemyHealth = SpawnedEnemy.GetComponentInChildren<Damageable>();
            IOpponentReceiver enemy = FindOpponentReceiver(SpawnedEnemy);
            IOpponentReceiver player = FindOpponentReceiver(SpawnedPlayer);

            if (playerHealth == null || enemyHealth == null || enemy == null)
            {
                Debug.LogError(
                    "Spawned gameplay prefabs are missing Damageable or opponent receiver components.",
                    this);
                Destroy(SpawnedPlayer);
                Destroy(SpawnedEnemy);
                SpawnedPlayer = null;
                SpawnedEnemy = null;
                return false;
            }

            enemy.SetOpponent(playerHealth);
            player?.SetOpponent(enemyHealth);

            HasStarted = true;
            return true;
        }

        private static GameObject InstantiateAt(GameObject prefab, Transform spawnPoint)
        {
            GameObject instance = Instantiate(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation);

            if (!instance.activeSelf)
                instance.SetActive(true);

            return instance;
        }

        private static IOpponentReceiver FindOpponentReceiver(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IOpponentReceiver receiver)
                    return receiver;
            }

            return null;
        }
    }
}
