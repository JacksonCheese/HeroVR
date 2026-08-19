using HeroVR.Abilities;
using HeroVR.Arena;
using HeroVR.Combat;
using HeroVR.Gameplay;
using HeroVR.Movement;
using HeroVR.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroVR.Editor
{
    public static class GameplayFoundationAssetBuilder
    {
        private const string ProjectilePrefabPath =
            "Assets/Prefabs/Abilities/EnergyProjectile.prefab";
        private const string PlayerPrefabPath =
            "Assets/Prefabs/Characters/DesktopPlayer.prefab";
        private const string EnemyPrefabPath =
            "Assets/Prefabs/Characters/TrainingEnemy.prefab";
        private const string MatchPrefabPath =
            "Assets/Prefabs/Gameplay/GameplayMatchBootstrap.prefab";
        private const string SandboxScenePath =
            "Assets/Scenes/Gameplay/GameplaySandbox.unity";

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

                EnergyProjectile projectilePrefab =
                    BuildProjectilePrefab(projectileMaterial);
                GameObject playerPrefab = BuildPlayerPrefab(projectilePrefab);
                GameObject enemyPrefab = BuildEnemyPrefab(enemyMaterial);
                BuildMatchPrefab(playerPrefab, enemyPrefab);
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
            BuildSandboxScene(useCleanUntitledScene ? buildScene : default);
            AssetDatabase.SaveAssets();
            Debug.Log("HeroVR gameplay foundation prefabs and sandbox scene built successfully.");
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

        private static void BuildMatchPrefab(
            GameObject playerPrefab,
            GameObject enemyPrefab)
        {
            GameObject root = new GameObject("GameplayMatchBootstrap");
            GameplayMatchBootstrap bootstrap =
                root.AddComponent<GameplayMatchBootstrap>();
            bootstrap.Configure(playerPrefab, enemyPrefab);

            PrefabUtility.SaveAsPrefabAsset(root, MatchPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildSandboxScene(Scene reusableUntitledScene)
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
                AssetDatabase.LoadAssetAtPath<GameObject>(MatchPrefabPath);
            PrefabUtility.InstantiatePrefab(matchPrefab, sandboxScene);

            EditorSceneManager.SaveScene(sandboxScene, SandboxScenePath);
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
