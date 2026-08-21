using UnityEngine;

namespace HeroVR.Bosses
{
    [DisallowMultipleComponent]
    public sealed class BossSpawnPoint : MonoBehaviour
    {
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private BossDefinition bossDefinition;

        public GameObject BossPrefab => bossPrefab;
        public BossDefinition BossDefinition => bossDefinition;

        public void Configure(GameObject prefab, BossDefinition definition)
        {
            bossPrefab = prefab;
            bossDefinition = definition;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(.75f, .12f, 1f, .9f);
            Gizmos.DrawWireCube(
                transform.position + Vector3.up * 2f,
                new Vector3(2f, 4f, 2f));
        }
    }
}
