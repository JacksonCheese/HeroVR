using HeroVR.Arena;
using UnityEngine;

namespace HeroVR.Enemies
{
    [DisallowMultipleComponent]
    public sealed class MinionSpawnPoint : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefabOverride;
        [SerializeField] private EnemyDefinition enemyDefinitionOverride;
        [SerializeField] private ArenaTeam team = ArenaTeam.TeamTwo;
        [SerializeField, Min(0f)] private float spawnRadius = 1f;
        [SerializeField, Min(0)] private int waveGroup;

        public GameObject EnemyPrefabOverride => enemyPrefabOverride;
        public EnemyDefinition EnemyDefinitionOverride => enemyDefinitionOverride;
        public ArenaTeam Team => team;
        public float SpawnRadius => spawnRadius;
        public int WaveGroup => waveGroup;

        public void Configure(
            GameObject prefab,
            EnemyDefinition definition,
            ArenaTeam spawnTeam,
            float radius,
            int group)
        {
            enemyPrefabOverride = prefab;
            enemyDefinitionOverride = definition;
            team = spawnTeam;
            spawnRadius = Mathf.Max(0f, radius);
            waveGroup = Mathf.Max(0, group);
        }

        public Vector3 GetSpawnPosition()
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(offset.x, 0f, offset.y);
        }

        private void OnValidate()
        {
            spawnRadius = Mathf.Max(0f, spawnRadius);
            waveGroup = Mathf.Max(0, waveGroup);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, .3f, .15f, .85f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(.15f, spawnRadius));
        }
    }
}
