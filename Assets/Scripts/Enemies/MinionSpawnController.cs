using System;
using System.Collections.Generic;
using HeroVR.Combat;
using UnityEngine;

namespace HeroVR.Enemies
{
    [DisallowMultipleComponent]
    public sealed class MinionSpawnController : MonoBehaviour
    {
        private sealed class SpawnedMinion
        {
            public GameObject Instance;
            public Damageable Health;
            public float CleanupTime = -1f;
        }

        [SerializeField] private GameObject defaultEnemyPrefab;
        [SerializeField] private EnemyDefinition defaultDefinition;
        [SerializeField] private MinionSpawnPoint[] spawnPoints;
        [SerializeField, Min(1)] private int maximumActiveMinions = 8;
        [SerializeField, Min(0f)] private float defeatedCleanupDelay = 3f;
        [SerializeField] private bool spawnOnStart;
        [SerializeField, Min(0)] private int startingGroupSize = 3;

        private readonly List<SpawnedMinion> activeMinions =
            new List<SpawnedMinion>();
        private Damageable target;
        private int nextSpawnPointIndex;
        private float nextCleanupCheck;

        public int ActiveCount => activeMinions.Count;
        public int MaximumActiveMinions => maximumActiveMinions;
        public event Action<GameObject> MinionSpawned;
        public event Action<GameObject> MinionRemoved;

        private void Start()
        {
            if (spawnOnStart)
                SpawnGroup(startingGroupSize);
        }

        private void Update()
        {
            if (Time.time < nextCleanupCheck)
                return;

            nextCleanupCheck = Time.time + .25f;
            CleanupDefeated();
        }

        public void Configure(
            GameObject enemyPrefab,
            EnemyDefinition definition,
            MinionSpawnPoint[] points,
            int activeLimit,
            float cleanupDelay,
            bool autoSpawn = false,
            int initialGroupSize = 0)
        {
            defaultEnemyPrefab = enemyPrefab;
            defaultDefinition = definition;
            spawnPoints = points ?? Array.Empty<MinionSpawnPoint>();
            maximumActiveMinions = Mathf.Max(1, activeLimit);
            defeatedCleanupDelay = Mathf.Max(0f, cleanupDelay);
            spawnOnStart = autoSpawn;
            startingGroupSize = Mathf.Max(0, initialGroupSize);
        }

        public void SetTarget(Damageable combatTarget)
        {
            target = combatTarget;
            for (int index = 0; index < activeMinions.Count; index++)
            {
                GenericEnemyBrain brain = activeMinions[index].Instance != null
                    ? activeMinions[index].Instance.GetComponent<GenericEnemyBrain>()
                    : null;
                brain?.SetTarget(target);
            }
        }

        public int SpawnGroup(int requestedCount, int waveGroup = -1)
        {
            if (requestedCount <= 0 || spawnPoints == null || spawnPoints.Length == 0)
                return 0;

            int availableSlots = maximumActiveMinions - activeMinions.Count;
            int spawnCount = Mathf.Min(requestedCount, availableSlots);
            int created = 0;
            for (int index = 0; index < spawnCount; index++)
            {
                MinionSpawnPoint point = NextSpawnPoint(waveGroup);
                if (point == null || !TrySpawn(point))
                    break;
                created++;
            }

            return created;
        }

        public void ResetSpawner(bool removeActiveMinions)
        {
            if (removeActiveMinions)
            {
                for (int index = activeMinions.Count - 1; index >= 0; index--)
                {
                    GameObject instance = activeMinions[index].Instance;
                    if (instance != null)
                        Destroy(instance);
                }
                activeMinions.Clear();
            }

            nextSpawnPointIndex = 0;
            nextCleanupCheck = 0f;
        }

        private bool TrySpawn(MinionSpawnPoint point)
        {
            GameObject prefab = point.EnemyPrefabOverride != null
                ? point.EnemyPrefabOverride
                : defaultEnemyPrefab;
            if (prefab == null)
                return false;

            GameObject instance = Instantiate(
                prefab,
                point.GetSpawnPosition(),
                point.transform.rotation);
            if (!instance.activeSelf)
                instance.SetActive(true);

            GenericEnemyBrain brain = instance.GetComponent<GenericEnemyBrain>();
            EnemyDefinition definition = point.EnemyDefinitionOverride != null
                ? point.EnemyDefinitionOverride
                : defaultDefinition;
            brain?.Configure(definition);
            brain?.SetTarget(target);

            Damageable health = instance.GetComponentInChildren<Damageable>();
            if (health == null)
            {
                Destroy(instance);
                return false;
            }

            activeMinions.Add(new SpawnedMinion
            {
                Instance = instance,
                Health = health
            });
            MinionSpawned?.Invoke(instance);
            return true;
        }

        private MinionSpawnPoint NextSpawnPoint(int waveGroup)
        {
            for (int attempt = 0; attempt < spawnPoints.Length; attempt++)
            {
                MinionSpawnPoint point = spawnPoints[nextSpawnPointIndex];
                nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
                if (point != null && (waveGroup < 0 || point.WaveGroup == waveGroup))
                    return point;
            }

            return null;
        }

        private void CleanupDefeated()
        {
            for (int index = activeMinions.Count - 1; index >= 0; index--)
            {
                SpawnedMinion minion = activeMinions[index];
                if (minion.Instance == null)
                {
                    activeMinions.RemoveAt(index);
                    continue;
                }

                if (!minion.Health.IsDead)
                {
                    minion.CleanupTime = -1f;
                    continue;
                }

                if (minion.CleanupTime < 0f)
                    minion.CleanupTime = Time.time + defeatedCleanupDelay;
                if (Time.time < minion.CleanupTime)
                    continue;

                GameObject removed = minion.Instance;
                activeMinions.RemoveAt(index);
                MinionRemoved?.Invoke(removed);
                Destroy(removed);
            }
        }

        private void OnValidate()
        {
            maximumActiveMinions = Mathf.Max(1, maximumActiveMinions);
            defeatedCleanupDelay = Mathf.Max(0f, defeatedCleanupDelay);
            startingGroupSize = Mathf.Max(0, startingGroupSize);
        }
    }
}
