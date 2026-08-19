using System.Collections;
using System.Collections.Generic;
using HeroVR.Abilities;
using HeroVR.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HeroVR.Tests
{
    public class CombatStabilityTests
    {
        [UnityTest]
        public IEnumerator Damageable_ReportsHitContextAndDiesOncePerLife()
        {
            GameObject attacker = new GameObject("Attacker");
            GameObject targetObject = new GameObject("Target");
            Damageable target = targetObject.AddComponent<Damageable>();
            int deathCount = 0;
            DamageInfo receivedDamage = default;

            target.Died += () => deathCount++;
            target.Damaged += damageInfo => receivedDamage = damageInfo;

            target.TakeDamage(new DamageInfo(
                25f,
                attacker,
                Vector3.one,
                Vector3.forward,
                4f));

            Assert.That(target.CurrentHealth, Is.EqualTo(75f));
            Assert.That(receivedDamage.Instigator, Is.SameAs(attacker));
            Assert.That(receivedDamage.Direction, Is.EqualTo(Vector3.forward));
            Assert.That(receivedDamage.KnockbackImpulse, Is.EqualTo(4f));

            target.TakeDamage(75f);
            target.TakeDamage(10f);

            Assert.That(target.IsDead, Is.True);
            Assert.That(deathCount, Is.EqualTo(1));

            target.ResetHealth();
            target.TakeDamage(100f);
            Assert.That(deathCount, Is.EqualTo(2));

            Object.Destroy(attacker);
            Object.Destroy(targetObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AreaDamage_DamagesMultiColliderTargetOnlyOnceAndSkipsInstigator()
        {
            GameObject instigator = new GameObject("Instigator");
            instigator.transform.position = Vector3.zero;
            Damageable instigatorHealth = instigator.AddComponent<Damageable>();
            instigator.AddComponent<SphereCollider>();

            GameObject targetObject = new GameObject("MultiColliderTarget");
            targetObject.transform.position = Vector3.right;
            Damageable target = targetObject.AddComponent<Damageable>();

            GameObject firstCollider = new GameObject("FirstCollider");
            firstCollider.transform.SetParent(targetObject.transform, false);
            firstCollider.AddComponent<SphereCollider>();

            GameObject secondCollider = new GameObject("SecondCollider");
            secondCollider.transform.SetParent(targetObject.transform, false);
            secondCollider.AddComponent<BoxCollider>();

            Physics.SyncTransforms();

            int damagedTargets = AreaDamage.Apply(
                Vector3.zero,
                3f,
                25f,
                0f,
                instigator,
                new Collider[16],
                new HashSet<Damageable>(),
                new HashSet<Rigidbody>());

            Assert.That(damagedTargets, Is.EqualTo(1));
            Assert.That(target.CurrentHealth, Is.EqualTo(75f));
            Assert.That(instigatorHealth.CurrentHealth, Is.EqualTo(100f));

            Object.Destroy(instigator);
            Object.Destroy(targetObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RespawnOnDeath_RestoresSpawnAndHealth()
        {
            GameObject actor = new GameObject("RespawningActor");
            Vector3 spawnPosition = new Vector3(2f, 1f, -3f);
            actor.transform.position = spawnPosition;

            Damageable health = actor.AddComponent<Damageable>();
            RespawnOnDeath respawn = actor.AddComponent<RespawnOnDeath>();
            respawn.SetRespawnDelay(0f);

            actor.transform.position = Vector3.one * 10f;
            health.TakeDamage(health.MaxHealth);

            Assert.That(health.IsDead, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(actor.transform.position, Is.EqualTo(spawnPosition));

            Object.Destroy(actor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnergyProjectile_IgnoresOwnerColliders()
        {
            GameObject owner = new GameObject("ProjectileOwner");
            Collider ownerCollider = owner.AddComponent<CapsuleCollider>();

            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Rigidbody projectileBody = projectileObject.AddComponent<Rigidbody>();
            projectileBody.useGravity = false;
            EnergyProjectile projectile = projectileObject.AddComponent<EnergyProjectile>();
            Collider projectileCollider = projectileObject.GetComponent<Collider>();

            projectile.Launch(Vector3.forward, owner);

            Assert.That(projectile.Owner, Is.SameAs(owner));
            Assert.That(
                Physics.GetIgnoreCollision(projectileCollider, ownerCollider),
                Is.True);

            Object.Destroy(owner);
            Object.Destroy(projectileObject);
            yield return null;
        }
    }
}
