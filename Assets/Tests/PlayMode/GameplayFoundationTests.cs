using System.Collections;
using HeroVR.Abilities;
using HeroVR.Arena;
using HeroVR.Combat;
using HeroVR.Gameplay;
using HeroVR.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HeroVR.Tests
{
    public class GameplayFoundationTests
    {
        [UnityTest]
        public IEnumerator AbilityLoadout_ActivatesAbilityAndEnforcesCooldown()
        {
            GameObject actor = new GameObject("AbilityOwner");
            actor.AddComponent<Damageable>();
            RadialSmashAbility smash = actor.AddComponent<RadialSmashAbility>();
            smash.SetCooldown(.05f);

            HeroAbilityLoadout loadout = actor.AddComponent<HeroAbilityLoadout>();
            loadout.Configure(smash, null, null, null);

            Assert.That(loadout.TryActivatePrimary(), Is.True);
            Assert.That(loadout.TryActivatePrimary(), Is.False);
            Assert.That(smash.CooldownRemaining, Is.GreaterThan(0f));

            yield return new WaitForSeconds(.06f);

            Assert.That(loadout.TryActivatePrimary(), Is.True);

            Object.Destroy(actor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ability_RejectsActivationWhileOwnerIsDead()
        {
            GameObject actor = new GameObject("DeadAbilityOwner");
            Damageable health = actor.AddComponent<Damageable>();
            RadialSmashAbility smash = actor.AddComponent<RadialSmashAbility>();

            health.TakeDamage(health.MaxHealth);

            Assert.That(smash.TryActivate(), Is.False);

            Object.Destroy(actor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CharacterKnockbackReceiver_MovesCharacterControllerFromDamage()
        {
            GameObject actor = new GameObject("KnockbackActor");
            actor.AddComponent<CharacterController>();
            Damageable health = actor.AddComponent<Damageable>();
            actor.AddComponent<CharacterKnockbackReceiver>();

            health.TakeDamage(new DamageInfo(
                1f,
                null,
                actor.transform.position,
                Vector3.right,
                5f));

            yield return null;

            Assert.That(actor.transform.position.x, Is.GreaterThan(0f));

            Object.Destroy(actor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DashAbility_UsesRequestedWorldDirection()
        {
            GameObject actor = new GameObject("DirectionalDashActor");
            actor.AddComponent<CharacterController>();
            actor.AddComponent<Damageable>();
            DashAbility dash = actor.AddComponent<DashAbility>();
            dash.SetDirection(Vector3.left);

            Assert.That(dash.TryActivate(), Is.True);
            Assert.That(actor.transform.position.x, Is.LessThan(-4.9f));
            Assert.That(Mathf.Abs(actor.transform.position.z), Is.LessThan(.01f));

            Object.Destroy(actor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MatchBootstrap_DiscoversSpawnPointsAndConnectsGenericTarget()
        {
            GameObject playerPrefab = new GameObject("PlayerPrefabSource");
            playerPrefab.AddComponent<Damageable>();
            playerPrefab.SetActive(false);

            GameObject enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyPrefab.name = "EnemyPrefabSource";
            enemyPrefab.AddComponent<Rigidbody>();
            enemyPrefab.AddComponent<Damageable>();
            enemyPrefab.AddComponent<RespawnOnDeath>();
            enemyPrefab.AddComponent<TrainingBot>();
            enemyPrefab.SetActive(false);

            Vector3 playerPosition = new Vector3(-3f, 0f, -4f);
            Vector3 enemyPosition = new Vector3(4f, 1f, 3f);
            ArenaSpawnPoint playerSpawn = CreateSpawnPoint(
                playerPosition,
                ArenaTeam.TeamOne,
                ArenaSpawnType.Player);
            ArenaSpawnPoint enemySpawn = CreateSpawnPoint(
                enemyPosition,
                ArenaTeam.TeamTwo,
                ArenaSpawnType.TrainingEnemy);

            GameObject bootstrapObject = new GameObject("MatchBootstrap");
            GameplayMatchBootstrap bootstrap =
                bootstrapObject.AddComponent<GameplayMatchBootstrap>();
            bootstrap.Configure(playerPrefab, enemyPrefab, false);

            Assert.That(bootstrap.TryStartMatch(), Is.True);
            Assert.That(bootstrap.HasStarted, Is.True);
            Assert.That(bootstrap.SpawnedPlayer.transform.position, Is.EqualTo(playerPosition));
            Assert.That(bootstrap.SpawnedEnemy.transform.position, Is.EqualTo(enemyPosition));

            Damageable spawnedPlayerHealth =
                bootstrap.SpawnedPlayer.GetComponent<Damageable>();
            TrainingBot spawnedEnemy =
                bootstrap.SpawnedEnemy.GetComponent<TrainingBot>();

            Assert.That(spawnedEnemy.Target, Is.SameAs(spawnedPlayerHealth));
            Assert.That(bootstrap.TryStartMatch(), Is.False);

            Object.Destroy(bootstrap.SpawnedPlayer);
            Object.Destroy(bootstrap.SpawnedEnemy);
            Object.Destroy(bootstrapObject);
            Object.Destroy(playerSpawn.gameObject);
            Object.Destroy(enemySpawn.gameObject);
            Object.Destroy(playerPrefab);
            Object.Destroy(enemyPrefab);
            yield return null;
        }

        private static ArenaSpawnPoint CreateSpawnPoint(
            Vector3 position,
            ArenaTeam team,
            ArenaSpawnType spawnType)
        {
            GameObject spawnObject = new GameObject($"{spawnType}Spawn");
            spawnObject.transform.position = position;
            ArenaSpawnPoint spawnPoint = spawnObject.AddComponent<ArenaSpawnPoint>();
            spawnPoint.Configure(team, 1, spawnType);
            return spawnPoint;
        }
    }
}
