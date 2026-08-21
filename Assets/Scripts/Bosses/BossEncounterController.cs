using HeroVR.Combat;
using HeroVR.Enemies;
using UnityEngine;

namespace HeroVR.Bosses
{
    [DisallowMultipleComponent]
    public sealed class BossEncounterController : MonoBehaviour
    {
        [SerializeField] private BossSpawnPoint spawnPoint;
        [SerializeField] private MinionSpawnController minionSpawner;
        [SerializeField] private bool startAutomatically;
        [SerializeField] private Damageable initialTarget;

        public BossController ActiveBoss { get; private set; }

        private void Start()
        {
            if (startAutomatically && initialTarget != null)
                StartEncounter(initialTarget);
        }

        public void Configure(
            BossSpawnPoint point,
            MinionSpawnController spawner,
            bool autoStart = false,
            Damageable target = null)
        {
            spawnPoint = point;
            minionSpawner = spawner;
            startAutomatically = autoStart;
            initialTarget = target;
        }

        public bool StartEncounter(Damageable target)
        {
            if (ActiveBoss != null || spawnPoint == null ||
                spawnPoint.BossPrefab == null || target == null)
            {
                return false;
            }

            GameObject instance = Instantiate(
                spawnPoint.BossPrefab,
                spawnPoint.transform.position,
                spawnPoint.transform.rotation);
            ActiveBoss = instance.GetComponent<BossController>();
            if (ActiveBoss == null)
            {
                Destroy(instance);
                return false;
            }

            ActiveBoss.Configure(spawnPoint.BossDefinition, minionSpawner);
            ActiveBoss.SetOpponent(target);
            minionSpawner?.SetTarget(target);
            return true;
        }

        public bool ResetEncounter(Damageable target)
        {
            if (ActiveBoss == null)
                return StartEncounter(target);

            ActiveBoss.SetOpponent(target);
            ActiveBoss.ResetEncounter();
            minionSpawner?.SetTarget(target);
            return true;
        }
    }
}
