using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Builds the destruction test area and the boss arena graybox.
    ///
    /// Both are new scenes. The production arena is never modified: destruction and boss combat
    /// need very different space to normal 1v1, and mixing them would make the graybox arena
    /// impossible to tune for either.
    ///
    /// Environment-owned throughout. Spawn points are bare transforms under GameplayHooks so
    /// gameplay can attach BossSpawnPoint, MinionSpawnPoint and DestructibleStructure without
    /// searching for arbitrary child names.
    /// </summary>
    public static class DestructionEnvironmentBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/Environment";
        private const string BreakableFolder = "Assets/Prefabs/Environment/Breakable";
        private const string PropFolder = "Assets/Prefabs/Environment/Props";

        private const string TestScenePath = "Assets/Scenes/Arenas/Arena_DestructionTest.unity";
        private const string BossScenePath = "Assets/Scenes/Arenas/BossArena_Graybox_01.unity";

        // --- Boss arena scale ------------------------------------------------------------
        // Sized for a boss of roughly 3x-6x human height, so up to about 11m. Exact size is
        // deliberately not encoded anywhere: the arena is built with clearance for that range
        // rather than for one specific boss.
        private const float BossZoneRadius = 22f;   // kept clear of columns so the boss can move
        private const float ArenaHalfExtent = 46f;
        private const float ArenaWallHeight = 28f;  // headroom above an 11m boss for flight
        private const float LowerWalkway = 7f;
        private const float UpperWalkway = 14f;
        private const float HumanDoorHeight = 2.6f; // player paths stay human-scale

        private static Material floorMaterial;
        private static Material wallMaterial;
        private static Material structureMaterial;
        private static Material walkwayMaterial;
        private static Material accentMaterial;

        // ---------------------------------------------------------------------------------
        // Priority 4: destruction test area
        // ---------------------------------------------------------------------------------

        [MenuItem("Tools/HeroVR/Environment/Build Destruction Test Area")]
        public static void BuildDestructionTestArea()
        {
            CreateMaterials();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Transform environment = new GameObject("Environment").transform;
            Transform hooks = new GameObject("GameplayHooks").transform;
            Transform lighting = new GameObject("Lighting").transform;

            // Courtyard floor with plenty of run-up either side of the walls, so a thrown enemy
            // can actually reach impact speed before hitting something.
            Box(environment, "Floor", new Vector3(0f, -.5f, 0f), new Vector3(46f, 1f, 34f), floorMaterial);

            Transform perimeter = Group(environment, "Perimeter");
            Box(perimeter, "Wall_North", new Vector3(0f, 4f, 17.5f), new Vector3(46f, 8f, 1f), wallMaterial);
            Box(perimeter, "Wall_South", new Vector3(0f, 4f, -17.5f), new Vector3(46f, 8f, 1f), wallMaterial);
            Box(perimeter, "Wall_East", new Vector3(23.5f, 4f, 0f), new Vector3(1f, 8f, 36f), wallMaterial);
            Box(perimeter, "Wall_West", new Vector3(-23.5f, 4f, 0f), new Vector3(1f, 8f, 36f), wallMaterial);

            // The breakthrough case: an exterior wall with clear space on both sides, so an enemy
            // thrown through it lands somewhere rather than immediately hitting the next surface.
            Transform breakables = Group(environment, "Breakables");
            PlaceBreakable(breakables, "Wall_Breakable_Concrete", "Exterior_Concrete",
                new Vector3(-8f, 0f, 4f), 0f);
            PlaceBreakable(breakables, "Wall_Breakable_Brick", "Exterior_Brick",
                new Vector3(8f, 0f, 4f), 0f);

            // Interior wall runs across the space, so breaking it opens a route between halves.
            PlaceBreakable(breakables, "Wall_Breakable_Interior", "Interior_Divider",
                new Vector3(0f, 0f, -6f), 90f);

            Transform props = Group(environment, "Props");
            PlaceProp(props, "Prop_Light_SmallCrate", new Vector3(-13f, .25f, -2f));
            PlaceProp(props, "Prop_Light_TrashCan", new Vector3(-11f, .45f, -3.4f));
            PlaceProp(props, "Prop_Medium_Barrel", new Vector3(12f, .55f, -2f));
            PlaceProp(props, "Prop_Medium_Bench", new Vector3(14f, .45f, -4.5f));
            PlaceProp(props, "Prop_Heavy_ConcreteChunk", new Vector3(-2f, .45f, 10f));
            PlaceProp(props, "Prop_Heavy_Car", new Vector3(6f, .85f, 11f));

            // Bare transforms only; gameplay decides what spawns here.
            Empty(hooks, "PlayerStart", new Vector3(0f, .1f, -14f), Quaternion.identity);
            Empty(hooks, "ThrowTarget_Concrete", new Vector3(-8f, 1.7f, 4f), Quaternion.Euler(0f, 180f, 0f));
            Empty(hooks, "ThrowTarget_Brick", new Vector3(8f, 1.7f, 4f), Quaternion.Euler(0f, 180f, 0f));
            Empty(hooks, "EnemySpawn_1", new Vector3(-6f, .1f, 12f), Quaternion.Euler(0f, 180f, 0f));
            Empty(hooks, "EnemySpawn_2", new Vector3(6f, .1f, 12f), Quaternion.Euler(0f, 180f, 0f));

            KeyLight(lighting);
            MarkStatic(environment.gameObject, skipBreakables: true);
            ConfigureRendering();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TestScenePath);

            Debug.Log("[DestructionEnvironmentBuilder] Wrote " + TestScenePath);
        }

        // ---------------------------------------------------------------------------------
        // Priority 5 and 6: boss arena
        // ---------------------------------------------------------------------------------

        [MenuItem("Tools/HeroVR/Environment/Build Boss Arena")]
        public static void BuildBossArena()
        {
            CreateMaterials();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Transform environment = new GameObject("Environment").transform;
            Transform hooks = new GameObject("GameplayHooks").transform;
            Transform lighting = new GameObject("Lighting").transform;

            BuildBossFloorAndWalls(environment);
            BuildBossVerticality(environment);
            BuildBossBreakables(environment);
            BuildBossProps(environment);
            BuildBossHooks(hooks);

            KeyLight(lighting);
            MarkStatic(environment.gameObject, skipBreakables: true);
            ConfigureRendering();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BossScenePath);

            Debug.Log("[DestructionEnvironmentBuilder] Wrote " + BossScenePath +
                      ". Boss zone radius " + BossZoneRadius + "m kept clear; wall height " +
                      ArenaWallHeight + "m.");
        }

        private static void BuildBossFloorAndWalls(Transform environment)
        {
            Box(environment, "Floor", new Vector3(0f, -.5f, 0f),
                new Vector3(ArenaHalfExtent * 2f + 4f, 1f, ArenaHalfExtent * 2f + 4f), floorMaterial);

            // Thick perimeter. A boss swing or a hurled car arrives fast, and thin walls are what
            // fast objects tunnel through between physics ticks.
            Transform perimeter = Group(environment, "Perimeter");
            float offset = ArenaHalfExtent + 1f;
            float span = ArenaHalfExtent * 2f + 4f;

            Box(perimeter, "Wall_North", new Vector3(0f, ArenaWallHeight * .5f, offset),
                new Vector3(span, ArenaWallHeight, 2f), wallMaterial);
            Box(perimeter, "Wall_South", new Vector3(0f, ArenaWallHeight * .5f, -offset),
                new Vector3(span, ArenaWallHeight, 2f), wallMaterial);
            Box(perimeter, "Wall_East", new Vector3(offset, ArenaWallHeight * .5f, 0f),
                new Vector3(2f, ArenaWallHeight, span), wallMaterial);
            Box(perimeter, "Wall_West", new Vector3(-offset, ArenaWallHeight * .5f, 0f),
                new Vector3(2f, ArenaWallHeight, span), wallMaterial);

            // Minion entry alcoves. Deliberately human-scale so players read them as their own
            // routes, and so a giant boss cannot follow a minion out through one.
            Transform alcoves = Group(environment, "MinionAlcoves");
            for (int index = 0; index < 6; index++)
            {
                float angle = index * 60f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 position = direction * (ArenaHalfExtent - 1.5f);

                Transform alcove = Group(alcoves, "Alcove_" + (char)('A' + index));
                alcove.localPosition = position;
                alcove.localRotation = Quaternion.Euler(0f, angle, 0f);

                Box(alcove, "Lintel", new Vector3(0f, HumanDoorHeight + .35f, 0f),
                    new Vector3(4f, .7f, 2.4f), structureMaterial);
                Box(alcove, "Jamb_L", new Vector3(-2.1f, HumanDoorHeight * .5f, 0f),
                    new Vector3(.8f, HumanDoorHeight, 2.4f), structureMaterial);
                Box(alcove, "Jamb_R", new Vector3(2.1f, HumanDoorHeight * .5f, 0f),
                    new Vector3(.8f, HumanDoorHeight, 2.4f), structureMaterial);
            }
        }

        /// <summary>
        /// Two walkway rings for Thor and Spider-Man to fight from. Both sit outside the boss zone
        /// radius so the boss has an unobstructed floor and does not clip a column every step.
        /// </summary>
        private static void BuildBossVerticality(Transform environment)
        {
            Transform vertical = Group(environment, "Verticality");

            BuildWalkwayRing(vertical, "LowerRing", LowerWalkway, ArenaHalfExtent - 7f);
            BuildWalkwayRing(vertical, "UpperRing", UpperWalkway, ArenaHalfExtent - 13f);

            // Corner towers give a route up and a high perch overlooking the boss zone.
            Transform towers = Group(vertical, "Towers");
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    string name = "Tower_" + (x < 0 ? "W" : "E") + (z < 0 ? "S" : "N");
                    Vector3 basePosition = new Vector3(x * (ArenaHalfExtent - 6f), 0f, z * (ArenaHalfExtent - 6f));

                    Transform tower = Group(towers, name);
                    tower.localPosition = basePosition;

                    Box(tower, "Shaft", new Vector3(0f, UpperWalkway * .5f, 0f),
                        new Vector3(5f, UpperWalkway, 5f), structureMaterial);
                    Box(tower, "Deck", new Vector3(0f, UpperWalkway + 3f, 0f),
                        new Vector3(8f, .6f, 8f), walkwayMaterial);
                    Box(tower, "Crown", new Vector3(0f, UpperWalkway + 6.5f, 0f),
                        new Vector3(6f, .5f, 6f), accentMaterial);
                }
            }
        }

        private static void BuildWalkwayRing(Transform parent, string name, float height, float radius)
        {
            Transform ring = Group(parent, name);

            // Four straight spans rather than a curved ring: simple box colliders, no seams for a
            // ragdoll or a dashing player to catch on.
            for (int index = 0; index < 4; index++)
            {
                float angle = index * 90f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                Transform span = Group(ring, "Span_" + (char)('A' + index));
                span.localPosition = direction * radius;
                span.localRotation = Quaternion.Euler(0f, angle, 0f);

                Box(span, "Deck", new Vector3(0f, height, 0f), new Vector3(26f, .7f, 6f), walkwayMaterial);

                // Support columns sit under the deck, still outside the boss zone.
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(span, "Support_" + (side < 0 ? "L" : "R"),
                        new Vector3(side * 9f, height * .5f, 0f),
                        new Vector3(1.8f, height, 1.8f), structureMaterial);
                }
            }
        }

        /// <summary>
        /// A limited, deliberately visible set of breakables. Not every surface: this needs to
        /// stay a performance-conscious prototype, and readable destruction beats ubiquitous
        /// destruction.
        /// </summary>
        private static void BuildBossBreakables(Transform environment)
        {
            Transform breakables = Group(environment, "Breakables");

            // Two wall sections positioned so an enemy thrown outward from the boss zone breaks
            // through into the outer ring, opening a real route.
            PlaceBreakable(breakables, "Wall_Breakable_Concrete", "BreakWall_North",
                new Vector3(0f, 0f, BossZoneRadius + 6f), 0f);
            PlaceBreakable(breakables, "Wall_Breakable_Concrete", "BreakWall_South",
                new Vector3(0f, 0f, -(BossZoneRadius + 6f)), 180f);

            // Barricades at the boss zone edge: cover that can be destroyed.
            PlaceBreakable(breakables, "Wall_Breakable_Brick", "Barricade_East",
                new Vector3(BossZoneRadius + 6f, 0f, 0f), 90f);
            PlaceBreakable(breakables, "Wall_Breakable_Brick", "Barricade_West",
                new Vector3(-(BossZoneRadius + 6f), 0f, 0f), 270f);

            // Breakable pillars on the diagonals, outside the clear boss floor.
            Transform pillars = Group(breakables, "BreakablePillars");
            float diagonal = (BossZoneRadius + 8f) * .707f;
            int pillarIndex = 0;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    pillarIndex++;
                    PlaceBreakable(pillars, "Wall_Breakable_Interior",
                        "BreakPillar_" + pillarIndex,
                        new Vector3(x * diagonal, 0f, z * diagonal),
                        Mathf.Atan2(x, z) * Mathf.Rad2Deg);
                }
            }
        }

        private static void BuildBossProps(Transform environment)
        {
            Transform props = Group(environment, "Props");

            // Heavy props near the boss for it to throw; lighter ones nearer the walkways.
            PlaceProp(props, "Prop_Heavy_Car", new Vector3(-12f, .85f, 14f));
            PlaceProp(props, "Prop_Heavy_Car", new Vector3(15f, .85f, -12f));
            PlaceProp(props, "Prop_Heavy_ConcreteChunk", new Vector3(9f, .45f, 16f));
            PlaceProp(props, "Prop_Heavy_ConcreteChunk", new Vector3(-16f, .45f, -9f));
            PlaceProp(props, "Prop_Medium_Barrel", new Vector3(-19f, .55f, 3f));
            PlaceProp(props, "Prop_Medium_Barrel", new Vector3(20f, .55f, 6f));
            PlaceProp(props, "Prop_Medium_Bench", new Vector3(4f, .45f, -19f));
            PlaceProp(props, "Prop_Light_SmallCrate", new Vector3(-6f, .25f, -17f));
            PlaceProp(props, "Prop_Light_TrashCan", new Vector3(17f, .45f, -17f));
        }

        /// <summary>
        /// Bare transforms only. Gameplay attaches BossSpawnPoint and MinionSpawnPoint; nothing
        /// here implements spawning.
        /// </summary>
        private static void BuildBossHooks(Transform hooks)
        {
            Empty(hooks, "ArenaCenter", Vector3.zero, Quaternion.identity);

            // Boss faces the players' entry side across the open floor.
            Empty(hooks, "BossSpawnPoint", new Vector3(0f, .1f, -(BossZoneRadius - 4f)),
                Quaternion.identity);

            Empty(hooks, "PlayerStart", new Vector3(0f, .1f, BossZoneRadius + 14f),
                Quaternion.Euler(0f, 180f, 0f));

            // Six minion spawns, paired per side, set back at the alcoves so minions enter from
            // several directions and never appear on top of the player.
            Transform minions = Group(hooks, "MinionSpawns");
            string[] labels = { "A1", "A2", "B1", "B2", "C1", "C2" };

            for (int index = 0; index < labels.Length; index++)
            {
                float angle = index * 60f + 30f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 position = direction * (ArenaHalfExtent - 5f);

                // Face inward toward the fight.
                Quaternion facing = Quaternion.LookRotation(-direction, Vector3.up);
                Empty(minions, "MinionSpawn_" + labels[index],
                    new Vector3(position.x, .1f, position.z), facing);
            }

            Empty(hooks, "SpectatorCamera_Ref", new Vector3(0f, 24f, -(ArenaHalfExtent + 12f)),
                Quaternion.Euler(26f, 0f, 0f));
        }

        // ---------------------------------------------------------------------------------
        // Shared helpers
        // ---------------------------------------------------------------------------------

        private static void PlaceBreakable(Transform parent, string prefabName, string instanceName,
            Vector3 position, float yaw)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                BreakableFolder + "/" + prefabName + ".prefab");

            if (prefab == null)
            {
                Debug.LogWarning("[DestructionEnvironmentBuilder] Missing breakable " + prefabName +
                                 ". Run Build Breakable Walls first.");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = instanceName;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static void PlaceProp(Transform parent, string prefabName, Vector3 position)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PropFolder + "/" + prefabName + ".prefab");

            if (prefab == null)
            {
                Debug.LogWarning("[DestructionEnvironmentBuilder] Missing prop " + prefabName +
                                 ". Run Build Throwable Props first.");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = position;
        }

        private static Transform Group(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject Box(Transform parent, string name, Vector3 position,
            Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static void Empty(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            GameObject empty = new GameObject(name);
            empty.transform.SetParent(parent, false);
            empty.transform.localPosition = position;
            empty.transform.localRotation = rotation;
        }

        private static void KeyLight(Transform parent)
        {
            GameObject lightObject = new GameObject("KeyLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(52f, -34f, 0f);

            Light key = lightObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, .96f, .9f);
            key.intensity = 1.15f;
            key.shadows = LightShadows.Soft;
        }

        /// <summary>
        /// Marks fixed geometry static for batching. Breakables are skipped: their states get
        /// toggled at runtime, and Unity warns when a static object is enabled or moved.
        /// </summary>
        private static void MarkStatic(GameObject root, bool skipBreakables)
        {
            const StaticEditorFlags flags = StaticEditorFlags.BatchingStatic
                                            | StaticEditorFlags.OccluderStatic
                                            | StaticEditorFlags.OccludeeStatic
                                            | StaticEditorFlags.ContributeGI;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (skipBreakables && IsUnder(child, "Breakables"))
                    continue;
                if (skipBreakables && IsUnder(child, "Props"))
                    continue;

                GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
            }
        }

        private static bool IsUnder(Transform transform, string ancestorName)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (current.name == ancestorName)
                    return true;
            }

            return false;
        }

        private static void ConfigureRendering()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.44f, .49f, .58f);
            RenderSettings.ambientEquatorColor = new Color(.35f, .36f, .4f);
            RenderSettings.ambientGroundColor = new Color(.17f, .17f, .19f);
            RenderSettings.fog = false;
        }

        private static void CreateMaterials()
        {
            floorMaterial = MakeMaterial("Env_BossFloor", new Color(.36f, .37f, .39f));
            wallMaterial = MakeMaterial("Env_BossWall", new Color(.25f, .26f, .29f));
            structureMaterial = MakeMaterial("Env_BossStructure", new Color(.31f, .32f, .35f));
            walkwayMaterial = MakeMaterial("Env_BossWalkway", new Color(.55f, .56f, .58f));
            accentMaterial = MakeMaterial("Env_BossAccent", new Color(.62f, .38f, .18f));

            AssetDatabase.SaveAssets();
        }

        private static Material MakeMaterial(string name, Color color)
        {
            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + name + ".mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", .12f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folderPath);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
