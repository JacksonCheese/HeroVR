using System.Collections;
using HeroVR.Combat;
using HeroVR.Heroes;
using HeroVR.Movement;
using HeroVR.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HeroVR.Tests
{
    public class ThorHammerFlightTests
    {
        private sealed class FlightRig
        {
            public GameObject Root;
            public Damageable Health;
            public RespawnOnDeath Respawn;
            public DesktopCharacterMotor Motor;
            public RecallableWeapon Weapon;
            public ManualWeaponMotionSource Motion;
            public ThorHammerFlight Flight;
            public ThorHammerFlightSettings Settings;
        }

        [UnityTest]
        public IEnumerator SpinThresholdAndDuration_ControlHoverActivation()
        {
            FlightRig rig = CreateRig();

            SetMotion(rig, Vector3.zero, 9f, true);
            rig.Flight.EvaluateMotion(.5f);
            Assert.That(rig.Flight.IsHovering, Is.False);

            SetMotion(rig, Vector3.zero, 13f, true);
            rig.Flight.EvaluateMotion(.1f);
            rig.Flight.EvaluateMotion(.1f);
            Assert.That(rig.Flight.IsHovering, Is.False);
            rig.Flight.EvaluateMotion(.1f);

            Assert.That(rig.Flight.IsSpinCharged, Is.True);
            Assert.That(rig.Flight.IsHovering, Is.True);

            DestroyRig(rig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HoverThreshold_UsesHysteresis()
        {
            FlightRig rig = CreateRig();
            ChargeSpin(rig);
            Assert.That(rig.Flight.IsHovering, Is.True);

            SetMotion(rig, Vector3.zero, 9f, true);
            rig.Flight.EvaluateMotion(.1f);
            Assert.That(
                rig.Flight.IsHovering,
                Is.True,
                "Spin between activation and deactivation thresholds should remain stable.");

            SetMotion(rig, Vector3.zero, 7f, true);
            rig.Flight.EvaluateMotion(.1f);
            Assert.That(rig.Flight.IsHovering, Is.False);

            DestroyRig(rig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChargedSpinAndDirectionalMotion_LaunchesOwner()
        {
            FlightRig rig = CreateRig();
            ChargeSpin(rig);
            Vector3 direction = new Vector3(.75f, .3f, .58f).normalized;
            Vector3 start = rig.Root.transform.position;

            SetMotion(rig, direction * 8f, 13f, true);
            rig.Flight.EvaluateMotion(.02f);

            Assert.That(rig.Flight.LaunchCount, Is.EqualTo(1));
            Assert.That(
                Vector3.Dot(rig.Flight.LastLaunchDirection, direction),
                Is.GreaterThan(.999f));
            Assert.That(rig.Motor.FlightVelocity.magnitude, Is.GreaterThan(1f));
            Assert.That(
                rig.Motor.FlightVelocity.magnitude,
                Is.LessThanOrEqualTo(rig.Settings.MaximumFlightSpeed + .001f));

            yield return null;
            Assert.That(
                Vector3.Dot(rig.Root.transform.position - start, direction),
                Is.GreaterThan(0f));

            DestroyRig(rig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StrongMotionWithoutSpin_DoesNotLaunch()
        {
            FlightRig rig = CreateRig();

            SetMotion(rig, Vector3.forward * 9f, 0f, true);
            rig.Flight.EvaluateMotion(.5f);

            Assert.That(rig.Flight.LaunchCount, Is.Zero);
            Assert.That(rig.Motor.FlightVelocity, Is.EqualTo(Vector3.zero));

            DestroyRig(rig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThrownHammer_EndsHoverAndRestoresGravitySmoothly()
        {
            FlightRig rig = CreateRig();
            yield return null;
            ChargeSpin(rig);
            rig.Flight.EvaluateMotion(.2f);
            float supportedGravityScale = rig.Flight.CurrentGravityScale;
            Assert.That(supportedGravityScale, Is.LessThan(1f));

            Assert.That(rig.Weapon.TryThrow(Vector3.forward * 4f), Is.True);
            SetMotion(rig, Vector3.zero, 13f, true);
            rig.Flight.EvaluateMotion(.1f);

            Assert.That(rig.Flight.IsHovering, Is.False);
            Assert.That(
                rig.Flight.CurrentGravityScale,
                Is.GreaterThan(supportedGravityScale));
            Assert.That(rig.Flight.CurrentGravityScale, Is.LessThan(1f));

            for (int index = 0; index < 8; index++)
                rig.Flight.EvaluateMotion(.1f);
            Assert.That(rig.Flight.CurrentGravityScale, Is.EqualTo(1f).Within(.001f));

            DestroyRig(rig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaunchImpulse_RespectsConfiguredSpeedCap()
        {
            FlightRig rig = CreateRig(maximumFlightSpeed: 6f, launchImpulse: 30f);
            ChargeSpin(rig);

            SetMotion(rig, Vector3.right * 100f, 13f, true);
            rig.Flight.EvaluateMotion(.02f);

            Assert.That(rig.Flight.LaunchCount, Is.EqualTo(1));
            Assert.That(
                rig.Motor.FlightVelocity.magnitude,
                Is.EqualTo(6f).Within(.001f));

            DestroyRig(rig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OwnerDeath_CancelsFlightAndReturnsHammer()
        {
            FlightRig rig = CreateRig(manualEvaluation: false);
            rig.Motor.AddFlightImpulse(Vector3.forward * 8f, 12f);
            Assert.That(rig.Weapon.TryThrow(Vector3.forward * 3f), Is.True);

            rig.Health.TakeDamage(rig.Health.MaxHealth);

            Assert.That(rig.Motor.FlightVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(rig.Flight.IsFlightActive, Is.False);
            Assert.That(rig.Weapon.State, Is.EqualTo(RecallableWeaponState.Held));

            DestroyRig(rig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TransformMotionSource_ExcludesOwnerLocomotion()
        {
            GameObject ownerObject = new GameObject("MotionReferenceOwner");
            ownerObject.transform.position = Vector3.up * 50f;
            Damageable owner = ownerObject.AddComponent<Damageable>();

            GameObject trackedObject = new GameObject("TrackedHammerPose");
            trackedObject.transform.SetParent(ownerObject.transform, false);
            trackedObject.transform.localPosition = Vector3.forward;

            GameObject weaponObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponObject.transform.SetParent(trackedObject.transform, false);
            weaponObject.AddComponent<Rigidbody>();
            RecallableWeapon weapon = weaponObject.AddComponent<RecallableWeapon>();
            weapon.SetHoldAnchor(trackedObject.transform);
            weapon.ConfigureOwner(owner);

            TransformWeaponMotionSource source =
                ownerObject.AddComponent<TransformWeaponMotionSource>();
            source.Configure(trackedObject.transform, weapon, null, 0f);
            yield return new WaitForFixedUpdate();

            ownerObject.transform.position += Vector3.right * 2f;
            yield return new WaitForFixedUpdate();
            Assert.That(
                source.CurrentMotion.LinearVelocity.magnitude,
                Is.LessThan(.01f),
                "Owner locomotion must not be interpreted as a hammer gesture.");

            trackedObject.transform.localPosition += Vector3.forward * .2f;
            trackedObject.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            yield return new WaitForFixedUpdate();
            Assert.That(source.CurrentMotion.LinearVelocity.magnitude, Is.GreaterThan(1f));
            Assert.That(source.CurrentMotion.SpinMagnitude, Is.GreaterThan(1f));

            Object.Destroy(ownerObject);
            yield return null;
        }

        private static FlightRig CreateRig(
            float maximumFlightSpeed = 15f,
            float launchImpulse = 11f,
            bool manualEvaluation = true)
        {
            GameObject root = new GameObject("ThorFlightTestRig");
            root.SetActive(false);
            root.transform.position = Vector3.up * 50f;
            root.AddComponent<CharacterController>();
            Damageable health = root.AddComponent<Damageable>();
            RespawnOnDeath respawn = root.AddComponent<RespawnOnDeath>();
            respawn.SetRespawnDelay(10f);
            DesktopCharacterMotor motor = root.AddComponent<DesktopCharacterMotor>();

            GameObject anchorObject = new GameObject("MjolnirAnchor");
            anchorObject.transform.SetParent(root.transform, false);
            anchorObject.transform.localPosition = Vector3.forward;

            GameObject weaponObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponObject.name = "FlightTestMjolnir";
            weaponObject.transform.SetParent(root.transform, false);
            weaponObject.AddComponent<Rigidbody>();
            RecallableWeapon weapon = weaponObject.AddComponent<RecallableWeapon>();
            weapon.SetHoldAnchor(anchorObject.transform);
            weapon.ConfigureOwner(health);

            ManualWeaponMotionSource motion =
                root.AddComponent<ManualWeaponMotionSource>();
            ThorHammerFlightSettings settings =
                ScriptableObject.CreateInstance<ThorHammerFlightSettings>();
            settings.Configure(
                10f,
                .3f,
                1f,
                .25f,
                5f,
                launchImpulse,
                3f,
                maximumFlightSpeed,
                .25f,
                12f,
                8f,
                .2f,
                4f,
                6f,
                4f,
                2f,
                .5f,
                0f,
                maximumFlightSpeed);
            ThorHammerFlight flight = root.AddComponent<ThorHammerFlight>();
            flight.enabled = !manualEvaluation;
            flight.Configure(settings, weapon, motion, motor);
            root.SetActive(true);

            FlightRig rig = new FlightRig
            {
                Root = root,
                Health = health,
                Respawn = respawn,
                Motor = motor,
                Weapon = weapon,
                Motion = motion,
                Flight = flight,
                Settings = settings
            };
            SetMotion(rig, Vector3.zero, 0f, true);
            return rig;
        }

        private static void ChargeSpin(FlightRig rig)
        {
            SetMotion(rig, Vector3.zero, 13f, true);
            rig.Flight.EvaluateMotion(.1f);
            rig.Flight.EvaluateMotion(.1f);
            rig.Flight.EvaluateMotion(.1f);
            rig.Flight.EvaluateMotion(.01f);
        }

        private static void SetMotion(
            FlightRig rig,
            Vector3 linearVelocity,
            float spinMagnitude,
            bool isHeld)
        {
            rig.Motion.SetMotion(
                linearVelocity,
                Vector3.up * spinMagnitude,
                isHeld,
                rig.Root);
        }

        private static void DestroyRig(FlightRig rig)
        {
            if (rig.Weapon != null && rig.Weapon.transform.root != rig.Root.transform)
                Object.Destroy(rig.Weapon.gameObject);
            Object.Destroy(rig.Root);
            Object.Destroy(rig.Settings);
        }
    }
}
