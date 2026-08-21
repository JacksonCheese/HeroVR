using System.Collections;
using System.Collections.Generic;
using HeroVR.Bosses;
using HeroVR.Combat;
using HeroVR.Destruction;
using HeroVR.Enemies;
using HeroVR.Interaction;
using HeroVR.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace HeroVR.Tests
{
    public sealed class PhysicsDestructionFoundationTests
    {
        [Test]
        public void ImpactDamageModel_UsesMassAndVelocityWithLowSpeedGate()
        {
            ImpactDamageResult slow = ImpactDamageModel.Calculate(
                5f, 1f, 1f, 3f, 6f, .7f, 100f, .5f, 30f, 1f);
            ImpactDamageResult lightFast = ImpactDamageModel.Calculate(
                2f, 8f, 8f, 3f, 6f, .7f, 100f, .5f, 30f, 1f);
            ImpactDamageResult heavyFast = ImpactDamageModel.Calculate(
                10f, 8f, 40f, 3f, 6f, .7f, 100f, .5f, 30f, 1f);

            Assert.That(slow.Damage, Is.Zero);
            Assert.That(lightFast.Damage, Is.GreaterThan(0f));
            Assert.That(heavyFast.Momentum, Is.GreaterThan(lightFast.Momentum));
            Assert.That(heavyFast.Damage, Is.GreaterThan(lightFast.Damage));
            Assert.That(
                ImpactSeverityUtility.Classify(heavyFast.ImpactStrength),
                Is.EqualTo(ImpactSeverity.Extreme));
        }

        [UnityTest]
        public IEnumerator Ragdoll_HeavyImpactDeathAndRespawnManageAiState()
        {
            GameObject actor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            actor.name = "RagdollTrainingBot";
            actor.transform.position = Vector3.up * 50f;
            actor.SetActive(false);
            actor.AddComponent<Rigidbody>();
            Damageable health = actor.AddComponent<Damageable>();
            RespawnOnDeath respawn = actor.AddComponent<RespawnOnDeath>();
            respawn.SetRespawnDelay(10f);
            NavMeshAgent agent = actor.AddComponent<NavMeshAgent>();
            agent.enabled = false;
            TrainingBot bot = actor.AddComponent<TrainingBot>();
            RagdollController ragdoll = actor.AddComponent<RagdollController>();
            ragdoll.Configure(18f, true, true, 10f, 0f, 10f, 8);
            actor.SetActive(true);

            health.TakeDamage(new DamageInfo(
                1f, null, actor.transform.position, Vector3.right, 2f, 5f));
            Assert.That(ragdoll.State, Is.EqualTo(RagdollState.Animated));
            Assert.That(bot.IsControlSuspended, Is.False);

            health.TakeDamage(new DamageInfo(
                1f, null, actor.transform.position, Vector3.right, 20f, 20f));
            Assert.That(ragdoll.State, Is.EqualTo(RagdollState.FullRagdoll));
            Assert.That(bot.IsControlSuspended, Is.True);

            respawn.RespawnNow();
            Assert.That(ragdoll.State, Is.EqualTo(RagdollState.Animated));
            Assert.That(bot.IsControlSuspended, Is.False);

            health.TakeDamage(new DamageInfo(
                health.MaxHealth,
                null,
                actor.transform.position,
                Vector3.back,
                1f,
                1f));
            Assert.That(ragdoll.State, Is.EqualTo(RagdollState.FullRagdoll));
            Assert.That(bot.IsControlSuspended, Is.True);

            respawn.RespawnNow();
            Assert.That(health.IsDead, Is.False);
            Assert.That(ragdoll.State, Is.EqualTo(RagdollState.Animated));
            Assert.That(bot.IsControlSuspended, Is.False);

            respawn.SetRespawnDelay(0f);
            health.TakeDamage(health.MaxHealth);
            Assert.That(health.IsDead, Is.False);
            Assert.That(
                ragdoll.State,
                Is.EqualTo(RagdollState.Animated),
                "A synchronous respawn must not be re-ragdolled by a later death subscriber.");

            Object.Destroy(actor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PhysicsGrab_EnforcesOwnershipAndTransfersReleaseVelocity()
        {
            GameObject firstHand = CreateGrabHand("FirstHand", Vector3.up * 50f);
            PhysicsGrabInteractor first =
                firstHand.GetComponent<PhysicsGrabInteractor>();
            GameObject secondHand = CreateGrabHand("SecondHand", Vector3.up * 50f);
            PhysicsGrabInteractor second =
                secondHand.GetComponent<PhysicsGrabInteractor>();

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "GrabbableProp";
            target.transform.position = Vector3.up * 50f;
            Rigidbody targetBody = target.AddComponent<Rigidbody>();
            targetBody.useGravity = false;
            target.AddComponent<ImpactDamageDealer>();
            ThrowableObject throwable = target.AddComponent<ThrowableObject>();
            throwable.Configure(1f, 30f);
            GrabbableObject grabbable = target.AddComponent<GrabbableObject>();
            Physics.SyncTransforms();

            Assert.That(first.TryBeginGrab(target.transform.position), Is.True);
            Assert.That(grabbable.IsGrabbed, Is.True);
            Assert.That(second.TryBeginGrab(target.transform.position), Is.False);

            Vector3 throwVelocity = new Vector3(12f, 2f, 0f);
            Assert.That(first.Release(throwVelocity), Is.True);
            Assert.That(grabbable.IsGrabbed, Is.False);
            Assert.That(throwable.LastInstigator, Is.SameAs(firstHand));
            Assert.That(targetBody.linearVelocity, Is.EqualTo(throwVelocity));
            Assert.That(first.HeldTarget, Is.Null);

            Object.Destroy(firstHand);
            Object.Destroy(secondHand);
            Object.Destroy(target);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CharacterGrab_ActivatesRagdollAndMakesBodyThrowable()
        {
            GameObject hand = CreateGrabHand("CharacterGrabHand", Vector3.up * 60f);
            PhysicsGrabInteractor interactor =
                hand.GetComponent<PhysicsGrabInteractor>();
            GameObject character = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            character.transform.position = Vector3.up * 60f;
            Rigidbody body = character.AddComponent<Rigidbody>();
            body.useGravity = false;
            character.AddComponent<Damageable>();
            RagdollController ragdoll = character.AddComponent<RagdollController>();
            ragdoll.Configure(18f, true, false, 10f, 0f, 10f, 8);
            GrabbableCharacter grabbable = character.AddComponent<GrabbableCharacter>();
            Physics.SyncTransforms();

            Assert.That(interactor.TryBeginGrab(character.transform.position), Is.True);
            Assert.That(ragdoll.State, Is.EqualTo(RagdollState.FullRagdoll));
            Assert.That(grabbable.IsGrabbed, Is.True);
            Assert.That(interactor.Release(Vector3.forward * 15f), Is.True);
            Assert.That(body.linearVelocity.z, Is.EqualTo(15f).Within(.01f));

            Object.Destroy(hand);
            Object.Destroy(character);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ImpactDealer_RoutesDamageInfoAndHonorsOwnerImmunity()
        {
            GameObject owner = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            owner.transform.position = Vector3.up * 70f;
            Damageable ownerHealth = owner.AddComponent<Damageable>();

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = Vector3.up * 70f + Vector3.forward * 2f;
            Damageable targetHealth = target.AddComponent<Damageable>();
            DamageInfo received = default;
            targetHealth.Damaged += info => received = info;

            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectile.transform.position = Vector3.up * 70f + Vector3.back * 2f;
            Rigidbody body = projectile.AddComponent<Rigidbody>();
            body.mass = 8f;
            body.useGravity = false;
            ImpactDamageDealer dealer = projectile.AddComponent<ImpactDamageDealer>();
            dealer.Configure(2f, 5f, .8f, 90f, .5f, 30f, 1f, 0f);
            dealer.SetInstigator(owner);

            Assert.That(
                dealer.TryApplyImpact(
                    owner.GetComponent<Collider>(),
                    Vector3.forward * 10f,
                    50f,
                    owner.transform.position),
                Is.False);
            Assert.That(ownerHealth.CurrentHealth, Is.EqualTo(ownerHealth.MaxHealth));

            Assert.That(
                dealer.TryApplyImpact(
                    target.GetComponent<Collider>(),
                    Vector3.forward * 10f,
                    50f,
                    target.transform.position),
                Is.True);
            Assert.That(targetHealth.CurrentHealth, Is.LessThan(targetHealth.MaxHealth));
            Assert.That(received.Instigator, Is.SameAs(owner));
            Assert.That(received.ImpactStrength, Is.GreaterThan(0f));
            Assert.That(received.DamageType, Is.EqualTo(DamageType.HeavyPhysical));

            Object.Destroy(owner);
            Object.Destroy(target);
            Object.Destroy(projectile);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Structure_RejectsLightHitAndTransitionsDamagedThenBrokenOnce()
        {
            GameObject root = new GameObject("TestStructure");
            root.SetActive(false);
            BoxCollider blocker = root.AddComponent<BoxCollider>();
            StructuralDamageReceiver receiver =
                root.AddComponent<StructuralDamageReceiver>();
            receiver.Configure(100f, 0f, 10f, .4f, .1f, 1f, .25f, 1.2f);
            GameObject intact = new GameObject("Intact");
            intact.transform.SetParent(root.transform);
            GameObject damaged = new GameObject("Damaged");
            damaged.transform.SetParent(root.transform);
            GameObject broken = new GameObject("Broken");
            broken.transform.SetParent(root.transform);
            DestructibleStructure structure = root.AddComponent<DestructibleStructure>();
            structure.Configure(
                .7f,
                intact,
                damaged,
                broken,
                new Collider[] { blocker });
            int brokenCount = 0;
            structure.Broken += () => brokenCount++;
            root.SetActive(true);

            Assert.That(
                receiver.TryReceiveDamage(new DamageInfo(
                    50f, null, Vector3.zero, Vector3.forward, 2f, 5f)),
                Is.False);
            Assert.That(structure.State, Is.EqualTo(StructureState.Intact));

            Assert.That(
                receiver.TryReceiveDamage(new DamageInfo(
                    40f,
                    null,
                    Vector3.zero,
                    Vector3.forward,
                    25f,
                    25f,
                    DamageType.HeavyPhysical)),
                Is.True);
            Assert.That(structure.State, Is.EqualTo(StructureState.Damaged));

            receiver.TryReceiveDamage(new DamageInfo(
                100f,
                null,
                Vector3.zero,
                Vector3.forward,
                35f,
                35f,
                DamageType.HeavyPhysical));
            receiver.TryReceiveDamage(new DamageInfo(
                100f,
                null,
                Vector3.zero,
                Vector3.forward,
                35f,
                35f,
                DamageType.HeavyPhysical));
            Assert.That(structure.State, Is.EqualTo(StructureState.Broken));
            Assert.That(blocker.enabled, Is.False);
            Assert.That(brokenCount, Is.EqualTo(1));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThrownBodyImpact_CanBreakStructuralReceiver()
        {
            GameObject wall = new GameObject("ThrownEnemyWall");
            BoxCollider wallCollider = wall.AddComponent<BoxCollider>();
            StructuralDamageReceiver receiver =
                wall.AddComponent<StructuralDamageReceiver>();
            receiver.Configure(35f, 0f, 5f, .5f, .1f, 1f, .2f, 1f);

            GameObject thrownBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Rigidbody body = thrownBody.AddComponent<Rigidbody>();
            body.mass = 10f;
            ImpactDamageDealer dealer = thrownBody.AddComponent<ImpactDamageDealer>();
            dealer.Configure(2f, 5f, .8f, 100f, .5f, 40f, 1f, 0f);

            Assert.That(
                dealer.TryApplyImpact(
                    wallCollider,
                    Vector3.forward * 9f,
                    60f,
                    wall.transform.position),
                Is.True);
            Assert.That(receiver.IsBroken, Is.True);
            Assert.That(receiver.LastDamageInfo.DamageType,
                Is.EqualTo(DamageType.HeavyPhysical));

            Object.Destroy(wall);
            Object.Destroy(thrownBody);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MinionSpawner_RespectsLimitAndCleansDefeatedMinion()
        {
            GameObject template = new GameObject("MinionTemplate");
            template.SetActive(false);
            template.AddComponent<Rigidbody>();
            template.AddComponent<Damageable>();

            GameObject pointObject = new GameObject("MinionPoint");
            pointObject.transform.position = Vector3.up * 80f;
            MinionSpawnPoint point = pointObject.AddComponent<MinionSpawnPoint>();
            point.Configure(template, null, HeroVR.Arena.ArenaTeam.TeamTwo, 0f, 0);

            GameObject controllerObject = new GameObject("MinionSpawner");
            MinionSpawnController spawner =
                controllerObject.AddComponent<MinionSpawnController>();
            spawner.Configure(
                template,
                null,
                new[] { point },
                2,
                0f);
            List<GameObject> spawned = new List<GameObject>();
            spawner.MinionSpawned += spawned.Add;

            Assert.That(spawner.SpawnGroup(5), Is.EqualTo(2));
            Assert.That(spawner.ActiveCount, Is.EqualTo(2));
            spawned[0].GetComponent<Damageable>().TakeDamage(100f);

            yield return new WaitForSeconds(.35f);
            Assert.That(spawner.ActiveCount, Is.EqualTo(1));

            spawner.ResetSpawner(true);
            Object.Destroy(template);
            Object.Destroy(pointObject);
            Object.Destroy(controllerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boss_RegionsPhasesAttackSummonDeathAndResetWork()
        {
            BossDefinition definition = ScriptableObject.CreateInstance<BossDefinition>();
            definition.Configure(
                "test-boss",
                "Test Boss",
                1000f,
                4f,
                0f,
                new[]
                {
                    new BossAttackSettings(
                        BossAttackType.Stomp,
                        0f,
                        0f,
                        5f,
                        10f,
                        5f)
                },
                new[] { new BossPhaseSettings(.8f, 2, 1) });

            GameObject bossObject = new GameObject("Boss");
            bossObject.SetActive(false);
            Damageable bossHealth = bossObject.AddComponent<Damageable>();
            BossController boss = bossObject.AddComponent<BossController>();
            boss.Configure(definition, null);
            GameObject headObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObject.transform.SetParent(bossObject.transform, false);
            BossHitRegion head = headObject.AddComponent<BossHitRegion>();
            head.Configure(boss, BossHitRegionType.Head, 1.5f);
            bossObject.SetActive(true);

            int phaseCount = 0;
            int summonCount = 0;
            int deathCount = 0;
            int resolvedAttacks = 0;
            boss.PhaseChanged += _ => phaseCount++;
            boss.MinionSummonRequested += (count, _) => summonCount += count;
            boss.BossDied += () => deathCount++;
            boss.AttackResolved += _ => resolvedAttacks++;

            Assert.That(
                CombatHitResolver.Apply(
                    headObject.GetComponent<Collider>(),
                    new DamageInfo(150f)),
                Is.EqualTo(1));
            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(775f));
            Assert.That(phaseCount, Is.EqualTo(1));
            Assert.That(summonCount, Is.EqualTo(2));

            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            targetObject.transform.position = bossObject.transform.position +
                Vector3.forward * 2f;
            Damageable target = targetObject.AddComponent<Damageable>();
            boss.SetOpponent(target);
            Physics.SyncTransforms();

            yield return null;
            yield return null;
            Assert.That(resolvedAttacks, Is.GreaterThanOrEqualTo(1));
            Assert.That(target.CurrentHealth, Is.LessThan(target.MaxHealth));

            bossHealth.TakeDamage(2000f);
            bossHealth.TakeDamage(10f);
            Assert.That(deathCount, Is.EqualTo(1));
            boss.ResetEncounter();
            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(1000f));
            Assert.That(boss.CurrentPhase, Is.Zero);

            Object.Destroy(bossObject);
            Object.Destroy(targetObject);
            Object.Destroy(definition);
            yield return null;
        }

        private static GameObject CreateGrabHand(string name, Vector3 position)
        {
            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = name;
            hand.transform.position = position;
            hand.transform.localScale = Vector3.one * .2f;
            Rigidbody body = hand.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            PhysicsGrabInteractor interactor = hand.AddComponent<PhysicsGrabInteractor>();
            interactor.Configure(null, hand, .5f, 1f, 30f);
            return hand;
        }
    }
}
