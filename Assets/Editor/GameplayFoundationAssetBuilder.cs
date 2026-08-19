using HeroVR.Abilities;
using HeroVR.Arena;
using HeroVR.Combat;
using HeroVR.Gameplay;
using HeroVR.Movement;
using HeroVR.Prototype;
using HeroVR.XR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

namespace HeroVR.Editor
{
    public static class GameplayFoundationAssetBuilder
    {
        private const string ProjectilePrefabPath =
            "Assets/Prefabs/Abilities/EnergyProjectile.prefab";
        private const string PlayerPrefabPath =
            "Assets/Prefabs/Characters/DesktopPlayer.prefab";
        private const string XRPlayerPrefabPath =
            "Assets/Prefabs/Characters/XRPlayer.prefab";
        private const string EnemyPrefabPath =
            "Assets/Prefabs/Characters/TrainingEnemy.prefab";
        private const string MatchPrefabPath =
            "Assets/Prefabs/Gameplay/GameplayMatchBootstrap.prefab";
        private const string XRMatchPrefabPath =
            "Assets/Prefabs/Gameplay/XRGameplayMatchBootstrap.prefab";
        private const string SandboxScenePath =
            "Assets/Scenes/Gameplay/GameplaySandbox.unity";
        private const string XRSandboxScenePath =
            "Assets/Scenes/Gameplay/XRGameplaySandbox.unity";

        [MenuItem("HeroVR/Build Gameplay Foundation Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException(
                    "Exit Play Mode before building gameplay foundation assets.");

            EnsureFolders();

            Scene originalActiveScene = SceneManager.GetActiveScene();
            bool useCleanUntitledScene =
                string.IsNullOrEmpty(originalActiveScene.path) &&
                !originalActiveScene.isDirty;

            if (string.IsNullOrEmpty(originalActiveScene.path) &&
                !useCleanUntitledScene)
            {
                throw new System.InvalidOperationException(
                    "Save or discard the current untitled scene before building gameplay assets.");
            }

            Scene buildScene = useCleanUntitledScene
                ? originalActiveScene
                : EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
            SceneManager.SetActiveScene(buildScene);

            try
            {
                Material projectileMaterial = LoadOrCreateMaterial(
                    "Assets/Materials/Gameplay/EnergyProjectile.mat",
                    new Color(.15f, .75f, 1f));
                Material enemyMaterial = LoadOrCreateMaterial(
                    "Assets/Materials/Gameplay/TrainingEnemy.mat",
                    new Color(.85f, .16f, .18f));
                Material handMaterial = LoadOrCreateMaterial(
                    "Assets/Materials/Gameplay/XRHand.mat",
                    new Color(.1f, .55f, 1f));

                EnergyProjectile projectilePrefab =
                    BuildProjectilePrefab(projectileMaterial);
                GameObject playerPrefab = BuildPlayerPrefab(projectilePrefab);
                GameObject xrPlayerPrefab = BuildXRPlayerPrefab(
                    projectilePrefab,
                    handMaterial);
                GameObject enemyPrefab = BuildEnemyPrefab(enemyMaterial);
                BuildMatchPrefab(
                    "GameplayMatchBootstrap",
                    MatchPrefabPath,
                    playerPrefab,
                    enemyPrefab);
                BuildMatchPrefab(
                    "XRGameplayMatchBootstrap",
                    XRMatchPrefabPath,
                    xrPlayerPrefab,
                    enemyPrefab);
            }
            finally
            {
                if (!useCleanUntitledScene)
                    EditorSceneManager.CloseScene(buildScene, true);

                if (!useCleanUntitledScene && originalActiveScene.IsValid())
                    SceneManager.SetActiveScene(originalActiveScene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene reusableScene = useCleanUntitledScene ? buildScene : default;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxScenePath) == null)
            {
                BuildSandboxScene(reusableScene, MatchPrefabPath, SandboxScenePath);
                reusableScene = default;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(XRSandboxScenePath) == null)
                BuildSandboxScene(reusableScene, XRMatchPrefabPath, XRSandboxScenePath);

            AssetDatabase.SaveAssets();
            Debug.Log(
                "HeroVR desktop and XR gameplay foundation assets built successfully.");
        }

        private static EnergyProjectile BuildProjectilePrefab(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "EnergyProjectile";
            root.transform.localScale = Vector3.one * .32f;
            root.GetComponent<Renderer>().sharedMaterial = material;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            root.AddComponent<EnergyProjectile>();

            PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<EnergyProjectile>(ProjectilePrefabPath);
        }

        private static GameObject BuildPlayerPrefab(EnergyProjectile projectilePrefab)
        {
            GameObject root = new GameObject("DesktopPlayer");

            CharacterController characterController =
                root.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = .4f;
            characterController.center = new Vector3(0f, .9f, 0f);

            root.AddComponent<Damageable>();
            root.AddComponent<RespawnOnDeath>();
            root.AddComponent<CharacterKnockbackReceiver>();
            DesktopCharacterMotor motor = root.AddComponent<DesktopCharacterMotor>();

            MeleePunchAbility punch = root.AddComponent<MeleePunchAbility>();
            ProjectileCaster projectile = root.AddComponent<ProjectileCaster>();
            DashAbility dash = root.AddComponent<DashAbility>();
            RadialSmashAbility smash = root.AddComponent<RadialSmashAbility>();
            HeroAbilityLoadout loadout = root.AddComponent<HeroAbilityLoadout>();

            GameObject cameraObject = new GameObject("HeroCamera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            GameObject spawnObject = new GameObject("ProjectileSpawn");
            spawnObject.transform.SetParent(cameraObject.transform, false);
            spawnObject.transform.localPosition = Vector3.forward * 1.1f;

            punch.SetCooldown(.35f);
            punch.SetAttackOrigin(cameraObject.transform);
            projectile.SetCooldown(.55f);
            projectile.Configure(projectilePrefab, spawnObject.transform, 24f);
            dash.SetCooldown(1.5f);
            dash.SetDirectionSource(root.transform);
            smash.SetCooldown(4f);
            smash.SetCenterPoint(root.transform);
            loadout.Configure(punch, projectile, dash, smash);
            motor.SetViewTransform(cameraObject.transform);

            DesktopHeroController controller =
                root.AddComponent<DesktopHeroController>();
            controller.ConfigureDesktopRig(
                camera,
                spawnObject.transform,
                motor,
                loadout);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        }

        private static GameObject BuildEnemyPrefab(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "TrainingEnemy";
            root.GetComponent<Renderer>().sharedMaterial = material;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 2.5f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            root.AddComponent<Damageable>();
            root.AddComponent<RespawnOnDeath>();
            root.AddComponent<TrainingBot>();

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        }

        private static GameObject BuildXRPlayerPrefab(
            EnergyProjectile projectilePrefab,
            Material handMaterial)
        {
            GameObject root = new GameObject("XRPlayer");

            CharacterController characterController =
                root.AddComponent<CharacterController>();
            characterController.height = 1.7f;
            characterController.radius = .3f;
            characterController.center = new Vector3(0f, .85f, 0f);

            root.AddComponent<Damageable>();
            root.AddComponent<RespawnOnDeath>();
            root.AddComponent<CharacterKnockbackReceiver>();

            MeleePunchAbility punch = root.AddComponent<MeleePunchAbility>();
            ProjectileCaster projectile = root.AddComponent<ProjectileCaster>();
            DashAbility dash = root.AddComponent<DashAbility>();
            RadialSmashAbility smash = root.AddComponent<RadialSmashAbility>();
            HeroAbilityLoadout loadout = root.AddComponent<HeroAbilityLoadout>();

            XROrigin xrOrigin = root.AddComponent<XROrigin>();
            XRCharacterMotor motor = root.AddComponent<XRCharacterMotor>();
            XRHeroInputAdapter input = root.AddComponent<XRHeroInputAdapter>();

            GameObject cameraOffset = new GameObject("CameraOffset");
            cameraOffset.transform.SetParent(root.transform, false);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(cameraOffset.transform, false);
            cameraObject.transform.localPosition = Vector3.up * 1.7f;
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = .05f;
            cameraObject.AddComponent<AudioListener>();
            ConfigureTrackedPose(
                cameraObject.AddComponent<TrackedPoseDriver>(),
                "Head",
                "<XRHMD>/centerEyePosition",
                "<XRHMD>/centerEyeRotation",
                "<XRHMD>/trackingState");

            Transform leftController = CreateTrackedController(
                cameraOffset.transform,
                "LeftController",
                "LeftHand",
                new Vector3(-.2f, 1.3f, .25f));
            Transform rightController = CreateTrackedController(
                cameraOffset.transform,
                "RightController",
                "RightHand",
                new Vector3(.2f, 1.3f, .25f));

            GameObject spawnObject = new GameObject("ProjectileSpawn");
            spawnObject.transform.SetParent(rightController, false);
            spawnObject.transform.localPosition = Vector3.forward * .12f;

            CreatePhysicsHand(
                root.transform,
                "LeftPhysicsHand",
                leftController,
                handMaterial);
            CreatePhysicsHand(
                root.transform,
                "RightPhysicsHand",
                rightController,
                handMaterial);

            xrOrigin.Origin = root;
            xrOrigin.CameraFloorOffsetObject = cameraOffset;
            xrOrigin.Camera = camera;
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            xrOrigin.CameraYOffset = 0f;

            punch.SetCooldown(.35f);
            punch.SetAttackOrigin(rightController);
            projectile.SetCooldown(.55f);
            projectile.Configure(projectilePrefab, spawnObject.transform, 24f);
            dash.SetCooldown(1.5f);
            dash.SetDirectionSource(cameraObject.transform);
            smash.SetCooldown(4f);
            smash.SetCenterPoint(root.transform);
            loadout.Configure(punch, projectile, dash, smash);
            motor.Configure(cameraObject.transform);

            input.Configure(
                CreateInputAction(
                    "Move",
                    InputActionType.Value,
                    "Vector2",
                    "<XRController>{LeftHand}/{primary2DAxis}"),
                CreateInputAction(
                    "Turn",
                    InputActionType.Value,
                    "Vector2",
                    "<XRController>{RightHand}/{primary2DAxis}"),
                CreateInputAction(
                    "Jump",
                    InputActionType.Button,
                    "Button",
                    "<XRController>{LeftHand}/{primaryButton}"),
                default,
                CreateInputAction(
                    "Energy Projectile",
                    InputActionType.Button,
                    "Button",
                    "<XRController>{RightHand}/{triggerButton}"),
                CreateInputAction(
                    "Dash",
                    InputActionType.Button,
                    "Button",
                    "<XRController>{LeftHand}/{primary2DAxisClick}"),
                CreateInputAction(
                    "Super Smash",
                    InputActionType.Button,
                    "Button",
                    "<XRController>{RightHand}/{primaryButton}"));

            PrefabUtility.SaveAsPrefabAsset(root, XRPlayerPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(XRPlayerPrefabPath);
        }

        private static Transform CreateTrackedController(
            Transform parent,
            string objectName,
            string handUsage,
            Vector3 editorPosition)
        {
            GameObject controllerObject = new GameObject(objectName);
            controllerObject.transform.SetParent(parent, false);
            controllerObject.transform.localPosition = editorPosition;

            ConfigureTrackedPose(
                controllerObject.AddComponent<TrackedPoseDriver>(),
                objectName,
                $"<XRController>{{{handUsage}}}/devicePosition",
                $"<XRController>{{{handUsage}}}/deviceRotation",
                $"<XRController>{{{handUsage}}}/trackingState");
            return controllerObject.transform;
        }

        private static void CreatePhysicsHand(
            Transform parent,
            string objectName,
            Transform trackingTarget,
            Material material)
        {
            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = objectName;
            hand.transform.SetParent(parent, false);
            hand.transform.SetPositionAndRotation(
                trackingTarget.position,
                trackingTarget.rotation);
            hand.transform.localScale = Vector3.one * .18f;
            hand.GetComponent<Renderer>().sharedMaterial = material;

            Rigidbody body = hand.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            TrackedHandPhysicsFollower follower =
                hand.AddComponent<TrackedHandPhysicsFollower>();
            follower.Configure(trackingTarget);
            hand.AddComponent<PunchHitbox>();
        }

        private static void ConfigureTrackedPose(
            TrackedPoseDriver driver,
            string actionPrefix,
            string positionPath,
            string rotationPath,
            string trackingStatePath)
        {
            driver.positionInput = CreateInputAction(
                $"{actionPrefix} Position",
                InputActionType.PassThrough,
                "Vector3",
                positionPath);
            driver.rotationInput = CreateInputAction(
                $"{actionPrefix} Rotation",
                InputActionType.PassThrough,
                "Quaternion",
                rotationPath);
            driver.trackingStateInput = CreateInputAction(
                $"{actionPrefix} Tracking State",
                InputActionType.PassThrough,
                "Integer",
                trackingStatePath);
            driver.ignoreTrackingState = false;
        }

        private static InputActionProperty CreateInputAction(
            string actionName,
            InputActionType actionType,
            string expectedControlType,
            string bindingPath)
        {
            InputAction action = new InputAction(
                actionName,
                actionType,
                bindingPath,
                expectedControlType: expectedControlType);
            return new InputActionProperty(action);
        }

        private static void BuildMatchPrefab(
            string objectName,
            string prefabPath,
            GameObject playerPrefab,
            GameObject enemyPrefab)
        {
            GameObject root = new GameObject(objectName);
            GameplayMatchBootstrap bootstrap =
                root.AddComponent<GameplayMatchBootstrap>();
            bootstrap.Configure(playerPrefab, enemyPrefab);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildSandboxScene(
            Scene reusableUntitledScene,
            string matchPrefabPath,
            string scenePath)
        {
            Scene originalActiveScene = SceneManager.GetActiveScene();
            bool reuseScene = reusableUntitledScene.IsValid();
            Scene sandboxScene = reuseScene
                ? reusableUntitledScene
                : EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
            SceneManager.SetActiveScene(sandboxScene);

            CreateCube(
                "Floor",
                new Vector3(0f, -.5f, 0f),
                new Vector3(26f, 1f, 26f));
            CreateCube("NorthWall", new Vector3(0f, 2f, 13f), new Vector3(26f, 5f, 1f));
            CreateCube("SouthWall", new Vector3(0f, 2f, -13f), new Vector3(26f, 5f, 1f));
            CreateCube("EastWall", new Vector3(13f, 2f, 0f), new Vector3(1f, 5f, 26f));
            CreateCube("WestWall", new Vector3(-13f, 2f, 0f), new Vector3(1f, 5f, 26f));

            CreateSpawnPoint(
                "PlayerSpawn",
                new Vector3(0f, .05f, -7f),
                Quaternion.identity,
                ArenaTeam.TeamOne,
                ArenaSpawnType.Player);
            CreateSpawnPoint(
                "EnemySpawn",
                new Vector3(0f, 1f, 6f),
                Quaternion.Euler(0f, 180f, 0f),
                ArenaTeam.TeamTwo,
                ArenaSpawnType.TrainingEnemy);

            CreatePhysicsProps();
            CreateLight();

            GameObject matchPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(matchPrefabPath);
            PrefabUtility.InstantiatePrefab(matchPrefab, sandboxScene);

            EditorSceneManager.SaveScene(sandboxScene, scenePath);
            if (!reuseScene)
                EditorSceneManager.CloseScene(sandboxScene, true);

            if (!reuseScene && originalActiveScene.IsValid())
                SceneManager.SetActiveScene(originalActiveScene);
        }

        private static void CreateSpawnPoint(
            string objectName,
            Vector3 position,
            Quaternion rotation,
            ArenaTeam team,
            ArenaSpawnType spawnType)
        {
            GameObject spawnObject = new GameObject(objectName);
            spawnObject.transform.SetPositionAndRotation(position, rotation);
            ArenaSpawnPoint spawnPoint = spawnObject.AddComponent<ArenaSpawnPoint>();
            spawnPoint.Configure(team, 1, spawnType);
        }

        private static void CreatePhysicsProps()
        {
            GameObject root = new GameObject("PhysicsProps");
            Vector3[] positions =
            {
                new Vector3(-5f, .6f, 0f),
                new Vector3(5f, .6f, 0f),
                new Vector3(-4f, .6f, 3f),
                new Vector3(4f, .6f, 3f),
                new Vector3(-5f, .6f, -4f),
                new Vector3(5f, .6f, -4f)
            };

            for (int index = 0; index < positions.Length; index++)
            {
                GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prop.name = $"PhysicsProp_{index + 1}";
                prop.transform.SetParent(root.transform);
                prop.transform.position = positions[index];
                prop.transform.localScale = Vector3.one * 1.2f;
                prop.AddComponent<Rigidbody>();
            }
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
        }

        private static GameObject CreateCube(
            string objectName,
            Vector3 position,
            Vector3 scale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            return cube;
        }

        private static Material LoadOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Abilities");
            EnsureFolder("Assets/Prefabs/Characters");
            EnsureFolder("Assets/Prefabs/Gameplay");
            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Materials/Gameplay");
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Scenes/Gameplay");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
