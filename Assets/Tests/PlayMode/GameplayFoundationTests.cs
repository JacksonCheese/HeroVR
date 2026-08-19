using System.Collections;
using HeroVR.Abilities;
using HeroVR.Arena;
using HeroVR.Combat;
using HeroVR.Gameplay;
using HeroVR.Heroes;
using HeroVR.Prototype;
using HeroVR.XR;
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
        public IEnumerator HeroProfile_AppliesStatsAndMomentumGatesUltimate()
        {
            HeroDefinition definition =
                ScriptableObject.CreateInstance<HeroDefinition>();

            GameObject actor = new GameObject("KineticVanguard");
            actor.SetActive(false);
            Damageable health = actor.AddComponent<Damageable>();
            RadialSmashAbility ultimate = actor.AddComponent<RadialSmashAbility>();
            HeroAbilityLoadout loadout = actor.AddComponent<HeroAbilityLoadout>();
            loadout.Configure(null, null, null, ultimate);
            HeroUltimateCharge charge = actor.AddComponent<HeroUltimateCharge>();
            HeroProfile profile = actor.AddComponent<HeroProfile>();
            profile.Configure(definition);
            actor.SetActive(true);

            Assert.That(health.MaxHealth, Is.EqualTo(125f));
            Assert.That(loadout.TryActivateUltimate(), Is.False);

            GameObject targetObject = new GameObject("MomentumTarget");
            Damageable target = targetObject.AddComponent<Damageable>();
            target.TakeDamage(new DamageInfo(40f, actor));
            Assert.That(charge.CurrentCharge, Is.EqualTo(40f));

            health.TakeDamage(new DamageInfo(20f, targetObject));
            Assert.That(charge.CurrentCharge, Is.EqualTo(50f));

            charge.AddCharge(50f);
            Assert.That(charge.IsUltimateReady, Is.True);
            Assert.That(loadout.TryActivateUltimate(), Is.True);
            Assert.That(charge.CurrentCharge, Is.Zero);

            Object.Destroy(actor);
            Object.Destroy(targetObject);
            Object.Destroy(definition);
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
        public IEnumerator XRCharacterMotor_UsesHeadRelativeMovementDirection()
        {
            GameObject actor = new GameObject("XRMovementActor");
            actor.AddComponent<CharacterController>();
            actor.AddComponent<Damageable>();
            XRCharacterMotor motor = actor.AddComponent<XRCharacterMotor>();

            GameObject headObject = new GameObject("Head");
            headObject.transform.SetParent(actor.transform, false);
            headObject.transform.localPosition = Vector3.up * 1.7f;
            headObject.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            motor.Configure(headObject.transform);
            motor.SetMoveInput(Vector2.up);

            Vector3 direction = motor.DesiredWorldMoveDirection;
            Assert.That(direction.x, Is.GreaterThan(.99f));
            Assert.That(Mathf.Abs(direction.z), Is.LessThan(.01f));

            Vector3 headPosition = headObject.transform.position;
            motor.RequestSnapTurn(1f);
            Assert.That(
                Vector3.Distance(headObject.transform.position, headPosition),
                Is.LessThan(.001f));

            Object.Destroy(actor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrackedHandFollower_ReportsTrackingVelocity()
        {
            GameObject actor = new GameObject("XRHandOwner");
            actor.AddComponent<Damageable>();

            GameObject target = new GameObject("TrackedController");
            target.transform.SetParent(actor.transform, false);

            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = "PhysicsHand";
            hand.transform.SetParent(actor.transform, false);
            hand.AddComponent<Rigidbody>();
            TrackedHandPhysicsFollower follower =
                hand.AddComponent<TrackedHandPhysicsFollower>();
            follower.Configure(target.transform);

            yield return new WaitForFixedUpdate();
            target.transform.position = Vector3.right * .1f;
            yield return new WaitForFixedUpdate();

            Assert.That(follower.Velocity.x, Is.GreaterThan(0f));
            Assert.That(follower.Velocity.magnitude, Is.LessThanOrEqualTo(20.01f));

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
