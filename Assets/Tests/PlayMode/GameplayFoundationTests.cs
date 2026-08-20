using System.Collections;
using HeroVR.Abilities;
using HeroVR.Arena;
using HeroVR.Combat;
using HeroVR.Gameplay;
using HeroVR.Heroes;
using HeroVR.Prototype;
using HeroVR.XR;
using HeroVR.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
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
        public IEnumerator DashAbility_TravelsOverMultipleFramesInRequestedDirection()
        {
            GameObject actor = new GameObject("DirectionalDashActor");
            actor.transform.position = Vector3.up * 50f;
            actor.AddComponent<CharacterController>();
            actor.AddComponent<Damageable>();
            DashAbility dash = actor.AddComponent<DashAbility>();
            dash.SetCooldown(1f);
            dash.SetDistance(5f);
            dash.SetDuration(.2f);
            dash.SetDirection(Vector3.left);

            Assert.That(dash.TryActivate(), Is.True);
            Vector3 startPosition = actor.transform.position;
            Assert.That(actor.transform.position, Is.EqualTo(startPosition));
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(dash.TryActivate(), Is.False, "Cooldown/state must reject overlap.");

            yield return null;

            float intermediateX = actor.transform.position.x;
            Assert.That(intermediateX, Is.LessThan(0f));
            Assert.That(intermediateX, Is.GreaterThan(-4.9f));

            int movingFrames = 1;
            float dashDeadline = Time.time + 1f;
            while (dash.IsDashing && Time.time < dashDeadline)
            {
                movingFrames++;
                yield return null;
            }

            Assert.That(movingFrames, Is.GreaterThan(1));
            Assert.That(
                actor.transform.position.x - startPosition.x,
                Is.EqualTo(-5f).Within(.15f));
            Assert.That(Mathf.Abs(actor.transform.position.z), Is.LessThan(.01f));

            Object.Destroy(actor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProjectileCaster_AlignsVelocityWithAimProviderDirection()
        {
            GameObject projectileTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileTemplate.name = "DirectionTestProjectile";
            Rigidbody templateBody = projectileTemplate.AddComponent<Rigidbody>();
            templateBody.useGravity = false;
            EnergyProjectile projectilePrefab =
                projectileTemplate.AddComponent<EnergyProjectile>();

            GameObject owner = new GameObject("ProjectileDirectionOwner");
            owner.AddComponent<Damageable>();
            ProjectileCaster caster = owner.AddComponent<ProjectileCaster>();

            GameObject aimObject = new GameObject("AimProvider");
            aimObject.transform.SetParent(owner.transform, false);
            Vector3 expectedDirection = new Vector3(.55f, .35f, .76f).normalized;
            aimObject.transform.rotation = Quaternion.LookRotation(expectedDirection);
            TransformAimProvider aimProvider =
                aimObject.AddComponent<TransformAimProvider>();
            aimProvider.Configure(aimObject.transform, aimObject.transform);

            caster.Configure(projectilePrefab, aimObject.transform, 18f);
            caster.SetAimProvider(aimProvider);
            caster.SetCooldown(0f);

            Assert.That(caster.TryActivate(), Is.True);
            EnergyProjectile spawned = caster.LastSpawnedProjectile;
            Assert.That(spawned, Is.Not.Null);
            Vector3 velocity = spawned.GetComponent<Rigidbody>().linearVelocity.normalized;
            Assert.That(Vector3.Dot(velocity, expectedDirection), Is.GreaterThan(.999f));

            Object.Destroy(spawned.gameObject);
            Object.Destroy(owner);
            Object.Destroy(projectileTemplate);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LightningAbility_UsesAimAndSharedDamageAttribution()
        {
            GameObject ownerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ownerObject.name = "LightningOwner";
            ownerObject.transform.position = Vector3.up * 60f;
            Damageable owner = ownerObject.AddComponent<Damageable>();

            GameObject aimObject = new GameObject("LightningAim");
            aimObject.transform.SetParent(ownerObject.transform, false);
            aimObject.transform.localPosition = Vector3.forward;
            Vector3 expectedDirection = Vector3.forward;
            TransformAimProvider aim = aimObject.AddComponent<TransformAimProvider>();
            aim.Configure(aimObject.transform, aimObject.transform);

            LightningAbility lightning = ownerObject.AddComponent<LightningAbility>();
            lightning.SetAimProvider(aim);
            lightning.ConfigureCombat(10f, 30f, 5f, 0f);
            lightning.SetCooldown(0f);

            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = "LightningTarget";
            targetObject.transform.position =
                ownerObject.transform.position + Vector3.forward * 4f;
            Damageable target = targetObject.AddComponent<Damageable>();
            DamageInfo receivedDamage = default;
            target.Damaged += info => receivedDamage = info;
            Physics.SyncTransforms();

            Assert.That(lightning.TryActivate(), Is.True);
            Assert.That(target.CurrentHealth, Is.EqualTo(70f));
            Assert.That(owner.CurrentHealth, Is.EqualTo(owner.MaxHealth));
            Assert.That(receivedDamage.Instigator, Is.SameAs(ownerObject));
            Assert.That(
                Vector3.Dot(lightning.LastDirection, expectedDirection),
                Is.GreaterThan(.999f));

            Object.Destroy(ownerObject);
            Object.Destroy(targetObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RecallableWeapon_TransitionsHeldThrownRecallingHeld()
        {
            GameObject ownerObject = new GameObject("MjolnirOwner");
            ownerObject.transform.position = Vector3.up * 50f;
            Damageable owner = ownerObject.AddComponent<Damageable>();
            ownerObject.AddComponent<CapsuleCollider>();

            GameObject anchorObject = new GameObject("WeaponAnchor");
            anchorObject.transform.SetParent(ownerObject.transform, false);
            anchorObject.transform.localPosition = Vector3.forward;

            GameObject weaponObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponObject.name = "StateTestMjolnir";
            weaponObject.AddComponent<Rigidbody>();
            RecallableWeapon weapon = weaponObject.AddComponent<RecallableWeapon>();
            weaponObject.AddComponent<PunchHitbox>();
            weapon.SetHoldAnchor(anchorObject.transform);
            weapon.ConfigureOwner(owner);
            weapon.ConfigureMotion(1f, 20f, 20f, 80f);

            Assert.That(weapon.State, Is.EqualTo(RecallableWeaponState.Held));
            Assert.That(weapon.TryThrow(Vector3.forward * 8f), Is.True);
            Assert.That(weapon.State, Is.EqualTo(RecallableWeaponState.Thrown));

            Rigidbody body = weaponObject.GetComponent<Rigidbody>();
            for (int index = 0; index < 18; index++)
                yield return new WaitForFixedUpdate();
            Assert.That(
                Vector3.Distance(body.position, anchorObject.transform.position),
                Is.GreaterThan(2.5f));
            Assert.That(weapon.BeginRecall(), Is.True);
            Assert.That(weapon.State, Is.EqualTo(RecallableWeaponState.Recalling));

            int recallFrames = 0;
            while (weapon.State != RecallableWeaponState.Held && recallFrames < 120)
            {
                recallFrames++;
                yield return new WaitForFixedUpdate();
            }

            Assert.That(recallFrames, Is.GreaterThan(1));
            Assert.That(weapon.State, Is.EqualTo(RecallableWeaponState.Held));
            Assert.That(weapon.transform.parent, Is.SameAs(anchorObject.transform));

            Object.Destroy(ownerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RecallableWeapon_AttributesOneImpactAndCannotDamageOwner()
        {
            GameObject ownerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ownerObject.name = "WeaponDamageOwner";
            ownerObject.transform.position = Vector3.up * 50f;
            Damageable owner = ownerObject.AddComponent<Damageable>();

            GameObject anchorObject = new GameObject("WeaponDamageAnchor");
            anchorObject.transform.SetParent(ownerObject.transform, false);
            anchorObject.transform.localPosition = Vector3.forward;

            GameObject weaponObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponObject.name = "DamageTestMjolnir";
            weaponObject.transform.localScale = Vector3.one * .25f;
            Rigidbody weaponBody = weaponObject.AddComponent<Rigidbody>();
            weaponBody.useGravity = false;
            RecallableWeapon weapon = weaponObject.AddComponent<RecallableWeapon>();
            weaponObject.AddComponent<PunchHitbox>();
            weapon.SetHoldAnchor(anchorObject.transform);
            weapon.ConfigureOwner(owner);
            weapon.ConfigureMotion(1f, 20f, 20f, 80f);
            weapon.ConfigureImpact(1f, 2f, 20f, 1f, 10f, .5f);

            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = "WeaponDamageTarget";
            targetObject.transform.position =
                ownerObject.transform.position + Vector3.forward * 3f;
            Damageable target = targetObject.AddComponent<Damageable>();
            DamageInfo receivedDamage = default;
            target.Damaged += info => receivedDamage = info;

            Assert.That(weapon.TryThrow(Vector3.forward * 10f), Is.True);
            weaponBody.useGravity = false;

            float deadline = Time.time + 1f;
            while (target.CurrentHealth >= target.MaxHealth && Time.time < deadline)
                yield return new WaitForFixedUpdate();

            Assert.That(target.CurrentHealth, Is.EqualTo(80f).Within(.5f));
            Assert.That(receivedDamage.Instigator, Is.SameAs(ownerObject));
            Assert.That(owner.CurrentHealth, Is.EqualTo(owner.MaxHealth));

            weaponBody.position =
                ownerObject.transform.position + Vector3.forward * 1.5f;
            weaponBody.linearVelocity = Vector3.forward * 10f;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(target.CurrentHealth, Is.EqualTo(80f).Within(.5f),
                "Per-target contact protection allowed a duplicate immediate hit.");

            Object.Destroy(ownerObject);
            Object.Destroy(weaponObject);
            Object.Destroy(targetObject);
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
            enemyPrefab.SetActive(false);
            enemyPrefab.AddComponent<Rigidbody>();
            enemyPrefab.AddComponent<Damageable>();
            enemyPrefab.AddComponent<RespawnOnDeath>();
            NavMeshAgent navigationAgent =
                enemyPrefab.AddComponent<NavMeshAgent>();
            navigationAgent.enabled = false;
            enemyPrefab.AddComponent<TrainingBot>();

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
