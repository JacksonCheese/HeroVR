using HeroVR.Abilities;
using HeroVR.Bosses;
using HeroVR.Combat;
using HeroVR.Destruction;
using HeroVR.Enemies;
using HeroVR.Input;
using HeroVR.Interaction;
using HeroVR.Prototype;
using HeroVR.XR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HeroVR.Editor
{
    public static class PhysicsDestructionAssetBuilder
    {
        private const string EnemyDefinitionPath =
            "Assets/Enemies/BasicMinion/BasicMinion.asset";
        private const string BossDefinitionPath =
            "Assets/Bosses/PlaceholderGiant/PlaceholderGiant.asset";
        private const string MinionPrefabPath =
            "Assets/Prefabs/Characters/PhysicsMinion.prefab";
        private const string BossPrefabPath =
            "Assets/Prefabs/Characters/PlaceholderGiantBoss.prefab";
        private const string ThrowablePropPrefabPath =
            "Assets/Prefabs/Gameplay/Physics/ThrowableHeavyProp.prefab";
        private const string BreakableWallPrefabPath =
            "Assets/Prefabs/Gameplay/Physics/BreakableTestWall.prefab";
        private const string SandboxScenePath =
            "Assets/Scenes/Gameplay/PhysicsDestructionSandbox.unity";

        [MenuItem("HeroVR/Build Physics Destruction Sandbox")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new System.InvalidOperationException(
                    "Exit Play Mode before building physics sandbox assets.");
            }

            EnsureFolders();
            EnemyDefinition enemyDefinition = BuildEnemyDefinition();
            BossDefinition bossDefinition = BuildBossDefinition();
            Material minionMaterial = LoadOrCreateMaterial(
                "Assets/Materials/Gameplay/PhysicsMinion.mat",
                new Color(.62f, .12f, .12f));
            Material bossMaterial = LoadOrCreateMaterial(
                "Assets/Materials/Gameplay/PlaceholderBoss.mat",
                new Color(.32f, .08f, .42f));
            Material intactMaterial = LoadOrCreateMaterial(
                "Assets/Materials/Gameplay/BreakableWallIntact.mat",
                new Color(.35f, .37f, .4f));
            Material damagedMaterial = LoadOrCreateMaterial(
                "Assets/Materials/Gameplay/BreakableWallDamaged.mat",
                new Color(.46f, .3f, .24f));
            Material propMaterial = LoadOrCreateMaterial(
                "Assets/Materials/Gameplay/ThrowableHeavyProp.mat",
                new Color(.18f, .42f, .5f));

            GameObject minionPrefab = BuildMinionPrefab(
                enemyDefinition,
                minionMaterial);
            GameObject bossPrefab = BuildBossPrefab(
                bossDefinition,
                bossMaterial);
            GameObject propPrefab = BuildThrowablePropPrefab(propMaterial);
            GameObject wallPrefab = BuildBreakableWallPrefab(
                intactMaterial,
                damagedMaterial);

            UpgradeXrGrabPrefab(
                "Assets/Prefabs/Characters/XRPlayer.prefab",
                true);
            UpgradeXrGrabPrefab(
                "Assets/Prefabs/Characters/ThorXRPlayer.prefab",
                false);
            UpgradeDesktopGrabPrefab(
                "Assets/Prefabs/Characters/DesktopPlayer.prefab");
            UpgradeDesktopGrabPrefab(
                "Assets/Prefabs/Characters/ThorDesktopPlayer.prefab");

            AssetDatabase.SaveAssets();
            BuildSandboxScene(
                minionPrefab,
                enemyDefinition,
                bossPrefab,
                bossDefinition,
                propPrefab,
                wallPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Physics/destruction sandbox built. Production arenas and " +
                "environment prefabs were not modified.");
        }

        private static EnemyDefinition BuildEnemyDefinition()
        {
            EnemyDefinition definition =
                AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
                definition.name = "Basic Physics Minion";
                AssetDatabase.CreateAsset(definition, EnemyDefinitionPath);
            }

            definition.Configure(
                "basic-physics-minion",
                "Basic Physics Minion",
                EnemyAttackRole.Melee,
                110f,
                5f,
                4.8f,
                11f,
                1.8f,
                12f,
                8f,
                18f,
                true,
                3.5f,
                7f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static BossDefinition BuildBossDefinition()
        {
            BossDefinition definition =
                AssetDatabase.LoadAssetAtPath<BossDefinition>(BossDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BossDefinition>();
                definition.name = "Placeholder Giant Boss";
                AssetDatabase.CreateAsset(definition, BossDefinitionPath);
            }

            definition.Configure(
                "placeholder-giant",
                "Placeholder Giant",
                1200f,
                4f,
                1.5f,
                new[]
                {
                    new BossAttackSettings(
                        BossAttackType.Stomp,
                        3.25f,
                        .8f,
                        7f,
                        30f,
                        24f)
                },
                new[]
                {
                    new BossPhaseSettings(.75f, 2, 0),
                    new BossPhaseSettings(.4f, 3, 1)
                });
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static GameObject BuildMinionPrefab(
            EnemyDefinition definition,
            Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "PhysicsMinion";
            root.SetActive(false);
            root.GetComponent<Renderer>().sharedMaterial = material;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = definition.BodyMass;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            root.AddComponent<Damageable>();
            root.AddComponent<RespawnOnDeath>();
            UnityEngine.AI.NavMeshAgent agent =
                root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.enabled = false;
            root.AddComponent<TrainingBot>();
            RagdollController ragdoll = root.AddComponent<RagdollController>();
            ragdoll.Configure(
                definition.RagdollImpactThreshold,
                true,
                definition.RecoversFromRagdoll,
                definition.RagdollRecoveryDelay,
                .35f,
                definition.CorpseCleanupDelay,
                8);
            GrabbableCharacter grabbable = root.AddComponent<GrabbableCharacter>();
            grabbable.Configure(true, true, true, 1.1f, 24f);
            root.AddComponent<ImpactDamageDealer>().Configure(
                3f,
                7f,
                .75f,
                90f,
                .7f,
                34f,
                1.15f,
                .2f);
            GenericEnemyBrain brain = root.AddComponent<GenericEnemyBrain>();
            brain.Configure(definition);
            root.SetActive(true);

            PrefabUtility.SaveAsPrefabAsset(root, MinionPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(MinionPrefabPath);
        }

        private static GameObject BuildBossPrefab(
            BossDefinition definition,
            Material material)
        {
            GameObject root = new GameObject("PlaceholderGiantBoss");
            root.SetActive(false);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            root.AddComponent<Damageable>();
            BossController controller = root.AddComponent<BossController>();
            controller.Configure(definition, null);

            CreateBossRegion(
                root.transform,
                controller,
                "TorsoRegion",
                PrimitiveType.Capsule,
                new Vector3(0f, 1.1f, 0f),
                new Vector3(.8f, 1.4f, .65f),
                BossHitRegionType.Torso,
                1f,
                material);
            CreateBossRegion(
                root.transform,
                controller,
                "HeadRegion",
                PrimitiveType.Sphere,
                new Vector3(0f, 2.65f, 0f),
                Vector3.one * .55f,
                BossHitRegionType.Head,
                1.5f,
                material);
            CreateBossRegion(
                root.transform,
                controller,
                "LeftLimbRegion",
                PrimitiveType.Cube,
                new Vector3(-.75f, 1.25f, 0f),
                new Vector3(.35f, 1.6f, .35f),
                BossHitRegionType.Limb,
                .8f,
                material);
            CreateBossRegion(
                root.transform,
                controller,
                "RightLimbRegion",
                PrimitiveType.Cube,
                new Vector3(.75f, 1.25f, 0f),
                new Vector3(.35f, 1.6f, .35f),
                BossHitRegionType.Limb,
                .8f,
                material);
            root.SetActive(true);

            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        }

        private static void CreateBossRegion(
            Transform parent,
            BossController controller,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            BossHitRegionType type,
            float multiplier,
            Material material)
        {
            GameObject regionObject = GameObject.CreatePrimitive(primitiveType);
            regionObject.name = name;
            regionObject.transform.SetParent(parent, false);
            regionObject.transform.localPosition = localPosition;
            regionObject.transform.localScale = localScale;
            regionObject.GetComponent<Renderer>().sharedMaterial = material;
            BossHitRegion region = regionObject.AddComponent<BossHitRegion>();
            region.Configure(controller, type, multiplier);
        }

        private static GameObject BuildThrowablePropPrefab(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "ThrowableHeavyProp";
            root.transform.localScale = Vector3.one * .9f;
            root.GetComponent<Renderer>().sharedMaterial = material;
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 8f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ImpactDamageDealer dealer = root.AddComponent<ImpactDamageDealer>();
            dealer.Configure(2.5f, 8f, .8f, 100f, .65f, 35f, 1f, .2f);
            ThrowableObject throwable = root.AddComponent<ThrowableObject>();
            throwable.Configure(1f, 26f);
            root.AddComponent<GrabbableObject>();

            PrefabUtility.SaveAsPrefabAsset(root, ThrowablePropPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ThrowablePropPrefabPath);
        }

        private static GameObject BuildBreakableWallPrefab(
            Material intactMaterial,
            Material damagedMaterial)
        {
            GameObject root = new GameObject("BreakableTestWall");
            BoxCollider blockingCollider = root.AddComponent<BoxCollider>();
            blockingCollider.center = new Vector3(0f, 2f, 0f);
            blockingCollider.size = new Vector3(5f, 4f, .6f);
            StructuralDamageReceiver receiver =
                root.AddComponent<StructuralDamageReceiver>();
            receiver.Configure(120f, 5f, 12f, .45f, .1f, 1f, .3f, 1.25f);

            GameObject intact = CreateVisualCube(
                root.transform,
                "IntactState",
                new Vector3(0f, 2f, 0f),
                new Vector3(5f, 4f, .6f),
                intactMaterial);
            GameObject damaged = CreateVisualCube(
                root.transform,
                "DamagedState",
                new Vector3(0f, 2f, 0f),
                new Vector3(5f, 4f, .6f),
                damagedMaterial);
            GameObject broken = new GameObject("BrokenState");
            broken.transform.SetParent(root.transform, false);
            CreateVisualCube(
                broken.transform,
                "LeftRemnant",
                new Vector3(-2.1f, 2f, 0f),
                new Vector3(.8f, 4f, .6f),
                damagedMaterial);
            CreateVisualCube(
                broken.transform,
                "RightRemnant",
                new Vector3(2.1f, 2f, 0f),
                new Vector3(.8f, 4f, .6f),
                damagedMaterial);
            CreateVisualCube(
                broken.transform,
                "TopRemnant",
                new Vector3(0f, 3.6f, 0f),
                new Vector3(3.4f, .8f, .6f),
                damagedMaterial);

            DebrisLifecycle[] debris = new DebrisLifecycle[2];
            for (int index = 0; index < debris.Length; index++)
            {
                GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = $"Debris_{index + 1}";
                chunk.transform.SetParent(root.transform, false);
                chunk.transform.localPosition = new Vector3(
                    index == 0 ? -.8f : .8f,
                    1.2f,
                    0f);
                chunk.transform.localScale = new Vector3(1.2f, 1f, .5f);
                chunk.GetComponent<Renderer>().sharedMaterial = damagedMaterial;
                Rigidbody chunkBody = chunk.AddComponent<Rigidbody>();
                chunkBody.isKinematic = true;
                debris[index] = chunk.AddComponent<DebrisLifecycle>();
                debris[index].Configure(5f, 16);
                chunk.SetActive(false);
            }

            DestructibleStructure structure = root.AddComponent<DestructibleStructure>();
            structure.Configure(
                .6f,
                intact,
                damaged,
                broken,
                new Collider[] { blockingCollider },
                debris);

            PrefabUtility.SaveAsPrefabAsset(root, BreakableWallPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(BreakableWallPrefabPath);
        }

        private static GameObject CreateVisualCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }

        private static void UpgradeXrGrabPrefab(string prefabPath, bool addBothHands)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (root == null)
                return;

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ConfigureXrGrabHand(contents, "LeftPhysicsHand", "LeftHand");
                if (addBothHands)
                    ConfigureXrGrabHand(contents, "RightPhysicsHand", "RightHand");
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigureXrGrabHand(
            GameObject playerRoot,
            string handName,
            string usage)
        {
            Transform hand = FindChild(playerRoot.transform, handName);
            if (hand == null)
                return;

            TrackedHandPhysicsFollower follower =
                hand.GetComponent<TrackedHandPhysicsFollower>();
            PhysicsGrabInteractor interactor =
                hand.GetComponent<PhysicsGrabInteractor>();
            if (interactor == null)
                interactor = hand.gameObject.AddComponent<PhysicsGrabInteractor>();
            interactor.Configure(follower, playerRoot, .32f, 1.15f, 24f);

            XRGrabInputAdapter input = hand.GetComponent<XRGrabInputAdapter>();
            if (input == null)
                input = hand.gameObject.AddComponent<XRGrabInputAdapter>();
            input.Configure(CreateInputAction(
                $"{usage} Grab",
                InputActionType.Button,
                "Button",
                $"<XRController>{{{usage}}}/{{gripButton}}"));
        }

        private static void UpgradeDesktopGrabPrefab(string prefabPath)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (root == null)
                return;

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (FindChild(contents.transform, "DesktopGrabHand") != null)
                    return;

                TransformAimProvider aim =
                    contents.GetComponentInChildren<TransformAimProvider>(true);
                if (aim == null)
                    return;

                GameObject hand = new GameObject("DesktopGrabHand");
                hand.transform.SetParent(contents.transform, false);
                SphereCollider collider = hand.AddComponent<SphereCollider>();
                collider.radius = .15f;
                collider.isTrigger = true;
                Rigidbody body = hand.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                PhysicsGrabInteractor interactor =
                    hand.AddComponent<PhysicsGrabInteractor>();
                interactor.Configure(null, contents, .75f, 1f, 24f);
                DesktopGrabInputAdapter input =
                    hand.AddComponent<DesktopGrabInputAdapter>();
                input.Configure(
                    CreateInputAction(
                        "Desktop Physics Grab",
                        InputActionType.Button,
                        "Button",
                        "<Keyboard>/e"),
                    aim,
                    2.25f,
                    16f);

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void BuildSandboxScene(
            GameObject minionPrefab,
            EnemyDefinition enemyDefinition,
            GameObject bossPrefab,
            BossDefinition bossDefinition,
            GameObject propPrefab,
            GameObject wallPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            CreateSolidCube(
                "Floor",
                new Vector3(0f, -.5f, 4f),
                new Vector3(32f, 1f, 32f),
                null);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Characters/ThorDesktopPlayer.prefab");
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(
                playerPrefab,
                scene);
            player.transform.SetPositionAndRotation(
                new Vector3(0f, .05f, -8f),
                Quaternion.identity);
            Damageable playerHealth = player.GetComponent<Damageable>();

            GameObject minion = (GameObject)PrefabUtility.InstantiatePrefab(
                minionPrefab,
                scene);
            minion.transform.SetPositionAndRotation(
                new Vector3(0f, 1f, -2f),
                Quaternion.Euler(0f, 180f, 0f));
            minion.GetComponent<GenericEnemyBrain>().SetTarget(playerHealth);

            GameObject wall = (GameObject)PrefabUtility.InstantiatePrefab(
                wallPrefab,
                scene);
            wall.transform.position = new Vector3(0f, 0f, 3f);

            for (int index = 0; index < 3; index++)
            {
                GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(
                    propPrefab,
                    scene);
                prop.transform.position = new Vector3(-4f + index * 4f, .6f, -4f);
            }

            GameObject spawningRoot = new GameObject("MinionSpawning");
            MinionSpawnPoint[] spawnPoints = new MinionSpawnPoint[4];
            Vector3[] pointPositions =
            {
                new Vector3(-7f, 1f, 9f),
                new Vector3(7f, 1f, 9f),
                new Vector3(-8f, 1f, 13f),
                new Vector3(8f, 1f, 13f)
            };
            for (int index = 0; index < spawnPoints.Length; index++)
            {
                GameObject pointObject = new GameObject($"MinionSpawn_{index + 1}");
                pointObject.transform.SetParent(spawningRoot.transform, false);
                pointObject.transform.position = pointPositions[index];
                spawnPoints[index] = pointObject.AddComponent<MinionSpawnPoint>();
                spawnPoints[index].Configure(
                    minionPrefab,
                    enemyDefinition,
                    HeroVR.Arena.ArenaTeam.TeamTwo,
                    1f,
                    index < 2 ? 0 : 1);
            }

            MinionSpawnController spawner =
                spawningRoot.AddComponent<MinionSpawnController>();
            spawner.Configure(
                minionPrefab,
                enemyDefinition,
                spawnPoints,
                6,
                4f);
            spawner.SetTarget(playerHealth);

            GameObject bossSpawnObject = new GameObject("BossSpawn");
            bossSpawnObject.transform.position = new Vector3(0f, 0f, 13f);
            bossSpawnObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            BossSpawnPoint bossSpawn = bossSpawnObject.AddComponent<BossSpawnPoint>();
            bossSpawn.Configure(bossPrefab, bossDefinition);

            GameObject encounterObject = new GameObject("BossEncounter");
            BossEncounterController encounter =
                encounterObject.AddComponent<BossEncounterController>();
            encounter.Configure(bossSpawn, spawner, true, playerHealth);

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;

            EditorSceneManager.SaveScene(scene, SandboxScenePath);
        }

        private static GameObject CreateSolidCube(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            if (material != null)
                cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static Transform FindChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index].name == name)
                    return children[index];
            }
            return null;
        }

        private static InputActionProperty CreateInputAction(
            string name,
            InputActionType type,
            string expectedControl,
            string binding)
        {
            return new InputActionProperty(new InputAction(
                name,
                type,
                binding,
                expectedControlType: expectedControl));
        }

        private static Material LoadOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"))
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
            EnsureFolder("Assets/Enemies");
            EnsureFolder("Assets/Enemies/BasicMinion");
            EnsureFolder("Assets/Bosses");
            EnsureFolder("Assets/Bosses/PlaceholderGiant");
            EnsureFolder("Assets/Prefabs/Gameplay/Physics");
            EnsureFolder("Assets/Scenes/Gameplay");
            EnsureFolder("Assets/Materials/Gameplay");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
