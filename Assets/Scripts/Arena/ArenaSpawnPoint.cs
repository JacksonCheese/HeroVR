using UnityEngine;

namespace HeroVR.Arena
{
    public enum ArenaTeam
    {
        Neutral = 0,
        TeamOne = 1,
        TeamTwo = 2
    }

    public enum ArenaSpawnType
    {
        Player = 0,
        TrainingEnemy = 1
    }

    [DisallowMultipleComponent]
    public sealed class ArenaSpawnPoint : MonoBehaviour
    {
        [SerializeField] private ArenaTeam team = ArenaTeam.Neutral;
        [SerializeField, Min(1)] private int playerSlot = 1;
        [SerializeField] private ArenaSpawnType spawnType;

        public ArenaTeam Team => team;
        public int PlayerSlot => playerSlot;
        public ArenaSpawnType SpawnType => spawnType;

        public void Configure(ArenaTeam spawnTeam, int slot, ArenaSpawnType type)
        {
            team = spawnTeam;
            playerSlot = Mathf.Max(1, slot);
            spawnType = type;
        }

        private void OnValidate()
        {
            playerSlot = Mathf.Max(1, playerSlot);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = spawnType == ArenaSpawnType.Player
                ? new Color(.1f, .65f, 1f, .9f)
                : new Color(1f, .25f, .2f, .9f);

            Gizmos.DrawWireSphere(transform.position + Vector3.up, .45f);
            Gizmos.DrawLine(
                transform.position + Vector3.up,
                transform.position + Vector3.up + transform.forward * 1.25f);
        }
    }
}
