using HeroVR.Abilities;
using HeroVR.Arena;
using HeroVR.Combat;
using HeroVR.Gameplay;
using HeroVR.Heroes;
using HeroVR.Input;
using HeroVR.Prototype;
using HeroVR.XR;
using HeroVR.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace HeroVR.Editor
{
    public static class ThorGameplayAssetBuilder
    {
        private const string ThorDefinitionPath = "Assets/Heroes/Thor/Thor.asset";
        private const string MjolnirPrefabPath = "Assets/Prefabs/Weapons/Mjolnir.prefab";
        private const string ThorPlayerPrefabPath =
            "Assets/Prefabs/Characters/ThorXRPlayer.prefab";
        private const string ThorDesktopPlayerPrefabPath =
            "Assets/Prefabs/Characters/ThorDesktopPlayer.prefab";
        private const string ThorMatchPrefabPath =
            "Assets/Prefabs/Gameplay/ThorXRGameplayMatchBootstrap.prefab";
        private const string ThorDesktopMatchPrefabPath =
            "Assets/Prefabs/Gameplay/ThorDesktopGameplayMatchBootstrap.prefab";
        private const string BaseArenaPath =
            "Assets/Scenes/Arenas/Arena_Graybox_01.unity";
        private const string ThorArenaPath =
            "Assets/Scenes/Arenas/Arena_ThorVRTest.unity";
        private const string ThorDesktopArenaPath =
            "Assets/Scenes/Arenas/Arena_ThorDesktopTest.unity";

        [MenuItem("HeroVR/Build Thor Gameplay Assets")]
        public static void Build()
        {
            GameplayFoundationAssetBuilder.Build();
            EnsureFolders();

            HeroDefinition thorDefinition = LoadOrCreateThorDefinition();
            Material hammerMaterial = LoadOrCreateMaterial(
                "Assets/Materials/Gameplay/Mjolnir.mat",
                new Color(.48f, .54f, .62f));
            Material handleMaterial = LoadOrCreateMaterial(
                "Assets/Materials/Gameplay/MjolnirHandle.mat",
                new Color(.24f, .12f, .055f));
            Material lightningMaterial = LoadOrCreateLineMaterial(
                "Assets/Materials/Gameplay/ThorLightning.mat",
                new Color(.35f, .75f, 1f));

            GameObject mjolnirPrefab = BuildMjolnirPrefab(
                hammerMaterial,
                handleMaterial);
            GameObject thorPlayerPrefab = BuildThorPlayerPrefab(
                thorDefinition,
                mjolnirPrefab,
                lightningMaterial);
            GameObject thorDesktopPlayerPrefab = BuildThorDesktopPlayerPrefab(
                thorDefinition,
                mjolnirPrefab,
                lightningMaterial);
            BuildMatchPrefab(
                thorPlayerPrefab,
                ThorMatchPrefabPath,
                "ThorXRGameplayMatchBootstrap");
            BuildMatchPrefab(
                thorDesktopPlayerPrefab,
                ThorDesktopMatchPrefabPath,
                "ThorDesktopGameplayMatchBootstrap");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            BuildThorArenaScene(ThorMatchPrefabPath, ThorArenaPath);
            BuildThorArenaScene(
                ThorDesktopMatchPrefabPath,
                ThorDesktopArenaPath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Thor gameplay assets built. Arena_Graybox_01 remains untouched; " +
                "use Arena_ThorVRTest or Arena_ThorDesktopTest for playtesting.");
        }

        private static HeroDefinition LoadOrCreateThorDefinition()
        {
            HeroDefinition definition =
                AssetDatabase.LoadAssetAtPath<HeroDefinition>(ThorDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<HeroDefinition>();
                definition.name = "Thor";
                AssetDatabase.CreateAsset(definition, ThorDefinitionPath);
            }

            definition.ConfigureIdentity(
                "thor",
                "Thor",
                "A durable thunder god who controls Mjolnir and channels aimed lightning.",
                new Color(.35f, .7f, 1f));

            SerializedObject serialized = new SerializedObject(definition);
            serialized.Update();
            SetString(serialized, "resourceName", "Storm Charge");
            SetString(serialized, "primaryName", "Mjolnir");
            SetString(serialized, "secondaryName", "Lightning Bolt");
            SetString(serialized, "movementName", "Thunder Dash");
            SetString(serialized, "ultimateName", "God of Thunder");
            SetFloat(serialized, "maxHealth", 190f);
            SetBool(serialized, "overrideLocomotion", true);
            SetNestedFloat(serialized, "locomotion", "moveSpeed", 5.4f);
            SetNestedFloat(serialized, "locomotion", "jumpHeight", 3.2f);
            SetFloat(serialized, "maximumUltimateCharge", 100f);
            SetFloat(serialized, "chargePerDamageDealt", 1f);
            SetFloat(serialized, "chargePerDamageTaken", .65f);

            SetNestedFloat(serialized, "melee", "cooldown", .38f);
            SetNestedFloat(serialized, "melee", "damage", 40f);
            SetNestedFloat(serialized, "melee", "range", 1.7f);
            SetNestedFloat(serialized, "melee", "radius", .75f);
            SetNestedFloat(serialized, "melee", "knockbackImpulse", 16f);

            SetNestedFloat(serialized, "physicalPunch", "minimumSpeed", 1.25f);
            SetNestedFloat(serialized, "physicalPunch", "damagePerSpeed", 9f);
            SetNestedFloat(serialized, "physicalPunch", "maximumDamage", 38f);
            SetNestedFloat(serialized, "physicalPunch", "knockbackMultiplier", 2.4f);
            SetNestedFloat(serialized, "physicalPunch", "maximumKnockbackImpulse", 18f);

            SetNestedFloat(serialized, "dash", "cooldown", 1.4f);
            SetNestedFloat(serialized, "dash", "distance", 5.5f);
            SetNestedFloat(serialized, "dash", "duration", .3f);

            SetNestedFloat(serialized, "weapon", "minimumHitSpeed", 1.1f);
            SetNestedFloat(serialized, "weapon", "damagePerSpeed", 12f);
            SetNestedFloat(serialized, "weapon", "maximumDamage", 60f);
            SetNestedFloat(serialized, "weapon", "knockbackMultiplier", 3.3f);
            SetNestedFloat(serialized, "weapon", "maximumKnockbackImpulse", 28f);
            SetNestedFloat(serialized, "weapon", "contactCooldown", .28f);
            SetNestedFloat(serialized, "weapon", "throwVelocityMultiplier", 1.45f);
            SetNestedFloat(serialized, "weapon", "maximumThrowSpeed", 28f);
            SetNestedFloat(serialized, "weapon", "recallSpeed", 22f);
            SetNestedFloat(serialized, "weapon", "recallAcceleration", 55f);

            SetNestedFloat(serialized, "lightning", "cooldown", .7f);
            SetNestedFloat(serialized, "lightning", "range", 26f);
            SetNestedFloat(serialized, "lightning", "damage", 34f);
            SetNestedFloat(serialized, "lightning", "knockbackImpulse", 10f);
            SetNestedFloat(serialized, "lightning", "visualDuration", .1f);

            SetNestedFloat(serialized, "ultimate", "cooldown", 1f);
            SetNestedFloat(serialized, "ultimate", "radius", 5f);
            SetNestedFloat(serialized, "ultimate", "damage", 50f);
            SetNestedFloat(serialized, "ultimate", "knockbackImpulse", 24f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static GameObject BuildMjolnirPrefab(
            Material hammerMaterial,
            Material handleMaterial)
        {
            GameObject root = new GameObject("Mjolnir");
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 4f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            BoxCollider handleCollider = root.AddComponent<BoxCollider>();
            handleCollider.center = new Vector3(0f, .23f, 0f);
            handleCollider.size = new Vector3(.12f, .6f, .12f);
            root.AddComponent<RecallableWeapon>();
            root.AddComponent<PunchHitbox>();

            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "HandleVisual";
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(0f, .23f, 0f);
            handle.transform.localScale = new Vector3(.11f, .6f, .11f);
            handle.GetComponent<Renderer>().sharedMaterial = handleMaterial;
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "HammerHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, .58f, 0f);
            head.transform.localScale = new Vector3(.5f, .28f, .3f);
            head.GetComponent<Renderer>().sharedMaterial = hammerMaterial;

            PrefabUtility.SaveAsPrefabAsset(root, MjolnirPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(MjolnirPrefabPath);
        }

        private static GameObject BuildThorPlayerPrefab(
            HeroDefinition definition,
            GameObject mjolnirPrefab,
            Material lightningMaterial)
        {
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Characters/XRPlayer.prefab");
            if (basePrefab == null)
                throw new System.InvalidOperationException("XRPlayer prefab is missing.");

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            PrefabUtility.UnpackPrefabInstance(
                root,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            root.name = "ThorXRPlayer";

            Damageable health = root.GetComponent<Damageable>();
            HeroAbilityLoadout loadout = root.GetComponent<HeroAbilityLoadout>();
            HeroProfile profile = root.GetComponent<HeroProfile>();
            MeleePunchAbility melee = root.GetComponent<MeleePunchAbility>();
            DashAbility dash = root.GetComponent<DashAbility>();
            RadialSmashAbility ultimate = root.GetComponent<RadialSmashAbility>();
            ProjectileCaster projectile = root.GetComponent<ProjectileCaster>();

            Transform rightController = FindChild(root.transform, "RightController");
            Transform rightAim = FindChild(root.transform, "RightAim");
            TransformAimProvider aimProvider =
                root.GetComponentInChildren<TransformAimProvider>(true);
            TrackedHandPhysicsFollower rightHandVelocity =
                FindChild(root.transform, "RightPhysicsHand")
                    .GetComponent<TrackedHandPhysicsFollower>();

            LightningAbility lightning = root.AddComponent<LightningAbility>();
            lightning.SetAimProvider(aimProvider);
            lightning.SetFallbackAimTransform(rightAim);

            GameObject lightningVisual = new GameObject("LightningVisual");
            lightningVisual.transform.SetParent(root.transform, false);
            LineRenderer line = lightningVisual.AddComponent<LineRenderer>();
            line.sharedMaterial = lightningMaterial;
            line.startWidth = .035f;
            line.endWidth = .012f;
            line.positionCount = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            lightning.SetLineRenderer(line);

            loadout.Configure(melee, lightning, dash, ultimate);
            if (projectile != null)
                Object.DestroyImmediate(projectile);

            Transform rightPhysicsHand = FindChild(root.transform, "RightPhysicsHand");
            PunchHitbox rightFistHitbox = rightPhysicsHand.GetComponent<PunchHitbox>();
            if (rightFistHitbox != null)
                Object.DestroyImmediate(rightFistHitbox);

            GameObject anchorObject = new GameObject("MjolnirAnchor");
            anchorObject.transform.SetParent(rightController, false);
            anchorObject.transform.localPosition = new Vector3(0f, -.06f, .04f);
            anchorObject.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);

            GameObject weaponObject = (GameObject)PrefabUtility.InstantiatePrefab(
                mjolnirPrefab,
                root.transform);
            weaponObject.transform.SetParent(anchorObject.transform, false);
            RecallableWeapon weapon = weaponObject.GetComponent<RecallableWeapon>();
            weapon.SetHoldAnchor(anchorObject.transform);
            weapon.ConfigureOwner(health);

            XRWeaponInputAdapter weaponInput =
                root.AddComponent<XRWeaponInputAdapter>();
            weaponInput.Configure(
                CreateInputAction(
                    "Mjolnir Grip",
                    InputActionType.Button,
                    "Button",
                    "<XRController>{RightHand}/{gripButton}"),
                CreateInputAction(
                    "Mjolnir Recall",
                    InputActionType.Button,
                    "Button",
                    "<XRController>{RightHand}/{secondaryButton}"),
                weapon,
                rightHandVelocity);

            XRHeroInputAdapter heroInput = root.GetComponent<XRHeroInputAdapter>();
            heroInput.Configure(
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
                    "<XRController>{RightHand}/{primaryButton}"),
                default,
                CreateInputAction(
                    "Lightning Bolt",
                    InputActionType.Button,
                    "Button",
                    "<XRController>{RightHand}/{triggerButton}"),
                CreateInputAction(
                    "Thunder Dash",
                    InputActionType.Button,
                    "Button",
                    "<XRController>{LeftHand}/{primary2DAxisClick}"),
                CreateInputAction(
                    "God of Thunder",
                    InputActionType.Button,
                    "Button",
                    "<XRController>{RightHand}/{primary2DAxisClick}"));

            TextMesh status = root.GetComponentInChildren<TextMesh>(true);
            if (status != null)
                status.color = definition.SignatureColor;

            profile.Configure(definition);
            PrefabUtility.SaveAsPrefabAsset(root, ThorPlayerPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ThorPlayerPrefabPath);
        }

        private static GameObject BuildThorDesktopPlayerPrefab(
            HeroDefinition definition,
            GameObject mjolnirPrefab,
            Material lightningMaterial)
        {
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Characters/DesktopPlayer.prefab");
            if (basePrefab == null)
            {
                throw new System.InvalidOperationException(
                    "DesktopPlayer prefab is missing.");
            }

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            PrefabUtility.UnpackPrefabInstance(
                root,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            root.name = "ThorDesktopPlayer";

            Damageable health = root.GetComponent<Damageable>();
            HeroAbilityLoadout loadout = root.GetComponent<HeroAbilityLoadout>();
            HeroProfile profile = root.GetComponent<HeroProfile>();
            DesktopHeroController desktopController =
                root.GetComponent<DesktopHeroController>();
            MeleePunchAbility melee = root.GetComponent<MeleePunchAbility>();
            DashAbility dash = root.GetComponent<DashAbility>();
            RadialSmashAbility ultimate = root.GetComponent<RadialSmashAbility>();
            ProjectileCaster projectile = root.GetComponent<ProjectileCaster>();
            TransformAimProvider aimProvider =
                root.GetComponentInChildren<TransformAimProvider>(true);
            Camera camera = root.GetComponentInChildren<Camera>(true);

            if (desktopController == null || aimProvider == null || camera == null)
            {
                throw new System.InvalidOperationException(
                    "DesktopPlayer prefab is missing its input or aim rig.");
            }

            LightningAbility lightning = root.AddComponent<LightningAbility>();
            lightning.SetAimProvider(aimProvider);
            lightning.SetFallbackAimTransform(camera.transform);

            GameObject lightningVisual = new GameObject("LightningVisual");
            lightningVisual.transform.SetParent(root.transform, false);
            LineRenderer line = lightningVisual.AddComponent<LineRenderer>();
            line.sharedMaterial = lightningMaterial;
            line.startWidth = .035f;
            line.endWidth = .012f;
            line.positionCount = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            lightning.SetLineRenderer(line);

            loadout.Configure(melee, lightning, dash, ultimate);
            if (projectile != null)
                Object.DestroyImmediate(projectile);

            GameObject anchorObject = new GameObject("MjolnirAnchor");
            anchorObject.transform.SetParent(camera.transform, false);
            anchorObject.transform.localPosition = new Vector3(.38f, -.38f, .72f);
            anchorObject.transform.localRotation = Quaternion.Euler(8f, 0f, -30f);

            GameObject weaponObject = (GameObject)PrefabUtility.InstantiatePrefab(
                mjolnirPrefab,
                root.transform);
            weaponObject.transform.SetParent(anchorObject.transform, false);
            RecallableWeapon weapon = weaponObject.GetComponent<RecallableWeapon>();
            weapon.SetHoldAnchor(anchorObject.transform);
            weapon.ConfigureOwner(health);

            DesktopWeaponInputAdapter weaponInput =
                root.AddComponent<DesktopWeaponInputAdapter>();
            weaponInput.Configure(
                CreateInputAction(
                    "Throw Mjolnir",
                    InputActionType.Button,
                    "Button",
                    "<Keyboard>/q"),
                CreateInputAction(
                    "Recall Mjolnir",
                    InputActionType.Button,
                    "Button",
                    "<Keyboard>/r"),
                weapon,
                aimProvider,
                18f);

            profile.Configure(definition);
            PrefabUtility.SaveAsPrefabAsset(root, ThorDesktopPlayerPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                ThorDesktopPlayerPrefabPath);
        }

        private static void BuildMatchPrefab(
            GameObject thorPlayerPrefab,
            string prefabPath,
            string objectName)
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Characters/TrainingEnemy.prefab");
            GameObject root = new GameObject(objectName);
            GameplayMatchBootstrap bootstrap =
                root.AddComponent<GameplayMatchBootstrap>();
            bootstrap.Configure(thorPlayerPrefab, enemyPrefab);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildThorArenaScene(
            string matchPrefabPath,
            string arenaPath)
        {
            Scene scene = EditorSceneManager.OpenScene(BaseArenaPath, OpenSceneMode.Single);
            ConfigureSpawn("TeamA_Spawn_1", ArenaTeam.TeamOne, 1, ArenaSpawnType.Player);
            ConfigureSpawn("TeamA_Spawn_2", ArenaTeam.TeamOne, 2, ArenaSpawnType.Player);
            ConfigureSpawn(
                "TeamB_Spawn_1",
                ArenaTeam.TeamTwo,
                1,
                ArenaSpawnType.TrainingEnemy);
            ConfigureSpawn(
                "TeamB_Spawn_2",
                ArenaTeam.TeamTwo,
                2,
                ArenaSpawnType.TrainingEnemy);

            GameObject matchPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(matchPrefabPath);
            PrefabUtility.InstantiatePrefab(matchPrefab, scene);
            EditorSceneManager.SaveScene(scene, arenaPath);
        }

        private static void ConfigureSpawn(
            string objectName,
            ArenaTeam team,
            int slot,
            ArenaSpawnType spawnType)
        {
            GameObject spawn = GameObject.Find(objectName);
            if (spawn == null)
                throw new System.InvalidOperationException($"Missing arena hook: {objectName}");

            ArenaSpawnPoint point = spawn.GetComponent<ArenaSpawnPoint>();
            if (point == null)
                point = spawn.AddComponent<ArenaSpawnPoint>();
            point.Configure(team, slot, spawnType);
            EditorUtility.SetDirty(point);
        }

        private static Transform FindChild(Transform parent, string objectName)
        {
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index].name == objectName)
                    return children[index];
            }

            throw new System.InvalidOperationException(
                $"Required XR prefab child is missing: {objectName}");
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

        private static Material LoadOrCreateLineMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
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
            EnsureFolder("Assets/Heroes/Thor");
            EnsureFolder("Assets/Prefabs/Weapons");
            EnsureFolder("Assets/Materials/Gameplay");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void SetString(
            SerializedObject serialized,
            string propertyName,
            string value)
        {
            serialized.FindProperty(propertyName).stringValue = value;
        }

        private static void SetBool(
            SerializedObject serialized,
            string propertyName,
            bool value)
        {
            serialized.FindProperty(propertyName).boolValue = value;
        }

        private static void SetFloat(
            SerializedObject serialized,
            string propertyName,
            float value)
        {
            serialized.FindProperty(propertyName).floatValue = value;
        }

        private static void SetNestedFloat(
            SerializedObject serialized,
            string parentName,
            string propertyName,
            float value)
        {
            serialized.FindProperty(parentName)
                .FindPropertyRelative(propertyName)
                .floatValue = value;
        }
    }
}
