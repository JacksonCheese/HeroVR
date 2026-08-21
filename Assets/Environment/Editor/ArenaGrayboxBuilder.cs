using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Editor-only generator for the first production graybox arena.
    ///
    /// This is environment tooling, not gameplay code: it authors saved assets (materials,
    /// environment prefabs, and a scene) and ships in an Editor folder so it is excluded from
    /// player builds. It intentionally contains no runtime components — spawn points are left as
    /// bare transforms for the gameplay-owned ArenaSpawnPoint component to be added later.
    ///
    /// Re-running the tool rebuilds the scene from scratch, so hand edits made in the Editor to
    /// Arena_Graybox_01 will be lost. Change the layout constants here instead.
    /// </summary>
    public static class ArenaGrayboxBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/Environment";
        private const string PrefabsFolder = "Assets/Prefabs/Environment";
        private const string ScenesFolder = "Assets/Scenes/Arenas";
        private const string ScenePath = ScenesFolder + "/Arena_Graybox_01.unity";

        // --- Arena footprint -------------------------------------------------------------
        // 56m x 56m of playable ground. Large enough that ranged powers have room to matter,
        // tight enough that a dash or two closes the distance for melee.
        private const float PlayableHalfExtent = 28f;
        private const float WallHeight = 12f;
        private const float WallThickness = 1.5f;

        // The player's jump is capped by DesktopCharacterMotor (jumpHeight 2.6, gravity -22) and
        // DashAbility is purely horizontal, so 2.6m is the hard vertical reach. Every jump-gated
        // hop is held to MaxHopRise, leaving margin for an imprecise takeoff. Exceeding the jump
        // height silently turns a tier into unreachable dead content.
        private const float PlayerJumpHeight = 2.6f;
        private const float MaxHopRise = 2f;

        // Elevation tiers. Tier 1 is ramp-accessible so no mobility power is required; tiers 2
        // and 3 sit one hop apart each, which is what gives the jump a reason to exist.
        private const float DeckHeight = 3.8f;                     // tier 1: wing decks + corner platforms
        private const float LedgeHeight = DeckHeight + MaxHopRise; // tier 2: tower ledges (5.8)
        private const float RoofHeight = LedgeHeight + MaxHopRise; // tier 3: tower roofs (7.8)
        private const float RoofThickness = .6f;

        private const float TowerCenterZ = 16f;
        private const float WingCenterX = 20f;

        // Height of a single walkable step. Must stay below the player CharacterController's
        // step offset or on-foot characters cannot climb it.
        private const float PlayerStepOffset = .3f; // DesktopPlayer.prefab CharacterController
        private const float StepRise = .25f;
        private const float PlazaTopHeight = StepRise * 2f;

        // Materials, cached across the build so every instance shares one material and batching
        // is not defeated by per-object copies.
        private static Material groundMaterial;
        private static Material plazaMaterial;
        private static Material wallMaterial;
        private static Material platformMaterial;
        private static Material rampMaterial;
        private static Material pillarMaterial;
        private static Material coverMaterial;
        private static Material teamAMaterial;
        private static Material teamBMaterial;

        [MenuItem("Tools/HeroVR/Environment/Build Graybox Arena")]
        public static void BuildArena()
        {
            CreateMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject coverPrefab = CreateCoverPrefab();
            GameObject pillarPrefab = CreatePillarPrefab();
            GameObject platformPrefab = CreatePlatformPrefab();
            GameObject rampPrefab = CreateRampPrefab();
            GameObject wallPrefab = CreateWallPrefab();

            Transform environment = new GameObject("Environment").transform;
            Transform hooks = new GameObject("GameplayHooks").transform;
            Transform lighting = new GameObject("Lighting").transform;

            BuildGround(environment);
            BuildPerimeter(environment, wallPrefab);
            BuildPlaza(environment);
            BuildTowers(environment);
            BuildWings(environment);
            BuildRamps(environment, rampPrefab);
            BuildCornerPlatforms(environment, platformPrefab);
            BuildPillars(environment, pillarPrefab);
            BuildCover(environment, coverPrefab);
            BuildSpawnMarkers(environment);

            BuildGameplayHooks(hooks);
            BuildLighting(lighting);

            MarkStaticRecursive(environment.gameObject);
            ConfigureSceneRendering();

            ValidateTraversal();

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ArenaGrayboxBuilder] Built " + ScenePath + " with " +
                      environment.GetComponentsInChildren<Renderer>().Length + " environment renderers.");
        }

        /// <summary>
        /// Guards the two ways this arena can silently become unplayable: a step taller than the
        /// player's CharacterController step offset (nothing can walk up it) or a hop taller than
        /// the player's jump (the tier above becomes unreachable dead content). Both shipped as
        /// real bugs once, so they are checked at build time rather than discovered in play.
        /// </summary>
        private static void ValidateTraversal()
        {
            WarnIfAbove("plaza step 1", StepRise, PlayerStepOffset);
            WarnIfAbove("plaza step 2", StepRise, PlayerStepOffset);
            WarnIfAbove("wing deck -> tower ledge", LedgeHeight - DeckHeight, PlayerJumpHeight);
            WarnIfAbove("tower ledge -> tower roof", RoofHeight - LedgeHeight, PlayerJumpHeight);

            Debug.Log(
                "[ArenaGrayboxBuilder] Traversal: ground -> plaza " + PlazaTopHeight +
                "m by " + StepRise + "m steps; ground -> deck " + DeckHeight +
                "m by ramp; deck -> ledge " + (LedgeHeight - DeckHeight) +
                "m hop; ledge -> roof " + (RoofHeight - LedgeHeight) +
                "m hop. Player limits: step " + PlayerStepOffset + "m, jump " + PlayerJumpHeight + "m.");
        }

        private static void WarnIfAbove(string label, float rise, float limit)
        {
            if (rise > limit)
            {
                Debug.LogError(
                    "[ArenaGrayboxBuilder] " + label + " rises " + rise +
                    "m, above the player limit of " + limit + "m. That surface is unreachable.");
            }
        }

        // ---------------------------------------------------------------------------------
        // Layout
        // ---------------------------------------------------------------------------------

        private static void BuildGround(Transform parent)
        {
            Transform group = Group("Ground", parent);

            // Slightly oversized so the perimeter walls sit on solid floor with no seam at the edge.
            Box("GroundSlab", group, new Vector3(0f, -0.5f, 0f),
                new Vector3(PlayableHalfExtent * 2f + 2f, 1f, PlayableHalfExtent * 2f + 2f), groundMaterial);
        }

        private static void BuildPerimeter(Transform parent, GameObject wallPrefab)
        {
            Transform group = Group("Perimeter", parent);

            float offset = PlayableHalfExtent + WallThickness * .5f;

            Instantiate(wallPrefab, "Wall_North", group, new Vector3(0f, WallHeight * .5f, offset), Quaternion.identity);
            Instantiate(wallPrefab, "Wall_South", group, new Vector3(0f, WallHeight * .5f, -offset), Quaternion.identity);
            Instantiate(wallPrefab, "Wall_East", group, new Vector3(offset, WallHeight * .5f, 0f), Quaternion.Euler(0f, 90f, 0f));
            Instantiate(wallPrefab, "Wall_West", group, new Vector3(-offset, WallHeight * .5f, 0f), Quaternion.Euler(0f, 90f, 0f));
        }

        /// <summary>
        /// Two shallow steps at the middle of the map, to read as the focal fight zone.
        ///
        /// Each riser is <see cref="StepRise"/> so both stay under the player CharacterController's
        /// 0.3 step offset. Taller steps made the plaza unreachable on foot: the player could not
        /// step up, and the Rigidbody-driven training bot wedged against the face indefinitely.
        /// </summary>
        private static void BuildPlaza(Transform parent)
        {
            Transform group = Group("Plaza", parent);

            Box("PlazaStepLower", group, new Vector3(0f, StepRise * .5f, 0f),
                new Vector3(22f, StepRise, 22f), plazaMaterial);
            Box("PlazaStepUpper", group, new Vector3(0f, StepRise, 0f),
                new Vector3(15f, StepRise * 2f, 15f), plazaMaterial);
        }

        /// <summary>
        /// North/south gate towers. Built as two legs plus a roof so there is a passage straight
        /// through at ground level — an alternate path and, later, something to fly through.
        /// </summary>
        private static void BuildTowers(Transform parent)
        {
            Transform group = Group("Towers", parent);

            BuildTower(group, "Tower_North", TowerCenterZ, 1f, teamAMaterial);
            BuildTower(group, "Tower_South", -TowerCenterZ, -1f, teamBMaterial);
        }

        private static void BuildTower(Transform parent, string towerName, float centerZ, float inwardSign, Material teamMaterial)
        {
            Transform tower = Group(towerName, parent);

            // Legs carry the roof, so their height is derived from it. They leave a 6m wide
            // passage between them, still tall enough to fly through later.
            float legHeight = RoofHeight - RoofThickness;
            Box("Leg_West", tower, new Vector3(-4.5f, legHeight * .5f, centerZ), new Vector3(3f, legHeight, 9f), wallMaterial);
            Box("Leg_East", tower, new Vector3(4.5f, legHeight * .5f, centerZ), new Vector3(3f, legHeight, 9f), wallMaterial);

            // Roof doubles as the arena's high ground, team-tinted so sides read at a glance.
            Box("Roof", tower, new Vector3(0f, RoofHeight - RoofThickness * .5f, centerZ),
                new Vector3(13f, RoofThickness, 9f), teamMaterial);

            // Inner ledge: a tier 2 perch on the plaza face, and the drop-down route off the roof.
            float innerZ = centerZ - inwardSign * 5.2f;
            Box("Ledge_Inner", tower, new Vector3(0f, LedgeHeight - .25f, innerZ), new Vector3(6f, .5f, 2.5f), platformMaterial);

            // Side ledges: the climb in from the corner platforms, one hop below the roof.
            Box("Ledge_SideWest", tower, new Vector3(-7.5f, LedgeHeight - .25f, centerZ), new Vector3(3f, .5f, 6f), platformMaterial);
            Box("Ledge_SideEast", tower, new Vector3(7.5f, LedgeHeight - .25f, centerZ), new Vector3(3f, .5f, 6f), platformMaterial);
        }

        /// <summary>East/west elevated walkways: the main flanking routes and ranged firing lines.</summary>
        private static void BuildWings(Transform parent)
        {
            Transform group = Group("Wings", parent);

            BuildWing(group, "Wing_East", WingCenterX);
            BuildWing(group, "Wing_West", -WingCenterX);
        }

        private static void BuildWing(Transform parent, string wingName, float centerX)
        {
            Transform wing = Group(wingName, parent);

            Box("Deck", wing, new Vector3(centerX, DeckHeight - .3f, 0f), new Vector3(10f, .6f, 26f), platformMaterial);

            // Visual supports so the deck does not read as floating. Simple boxes, no Rigidbodies.
            for (int index = -1; index <= 1; index++)
            {
                Box("Support_" + (index + 2), wing,
                    new Vector3(centerX, (DeckHeight - .6f) * .5f, index * 10f),
                    new Vector3(1.5f, DeckHeight - .6f, 1.5f), wallMaterial);
            }
        }

        /// <summary>
        /// Four ramps give every player a no-mobility-required route to tier 1. Placed on the
        /// outer edges so they never cut through the plaza.
        /// </summary>
        private static void BuildRamps(Transform parent, GameObject rampPrefab)
        {
            Transform group = Group("Ramps", parent);

            float baseZ = 17.5f;

            Instantiate(rampPrefab, "Ramp_EastSouth", group, new Vector3(WingCenterX, 0f, -baseZ), Quaternion.identity);
            Instantiate(rampPrefab, "Ramp_EastNorth", group, new Vector3(WingCenterX, 0f, baseZ), Quaternion.Euler(0f, 180f, 0f));
            Instantiate(rampPrefab, "Ramp_WestSouth", group, new Vector3(-WingCenterX, 0f, -baseZ), Quaternion.identity);
            Instantiate(rampPrefab, "Ramp_WestNorth", group, new Vector3(-WingCenterX, 0f, baseZ), Quaternion.Euler(0f, 180f, 0f));
        }

        /// <summary>Corner stepping stones linking the wing decks toward the tower side ledges.</summary>
        private static void BuildCornerPlatforms(Transform parent, GameObject platformPrefab)
        {
            Transform group = Group("CornerPlatforms", parent);

            float offset = 14.5f;
            int index = 0;

            for (int signX = -1; signX <= 1; signX += 2)
            {
                for (int signZ = -1; signZ <= 1; signZ += 2)
                {
                    index++;
                    Instantiate(platformPrefab, "CornerPlatform_" + index, group,
                        new Vector3(signX * offset, DeckHeight - .3f, signZ * offset), Quaternion.identity);
                }
            }
        }

        /// <summary>
        /// Tall pillars break long sightlines across the plaza and give future climbing/web
        /// mechanics a vertical surface to attach to.
        /// </summary>
        private static void BuildPillars(Transform parent, GameObject pillarPrefab)
        {
            Transform group = Group("Pillars", parent);

            float offset = 10f;
            int index = 0;

            for (int signX = -1; signX <= 1; signX += 2)
            {
                for (int signZ = -1; signZ <= 1; signZ += 2)
                {
                    index++;
                    Instantiate(pillarPrefab, "Pillar_" + index, group,
                        new Vector3(signX * offset, 5f, signZ * offset), Quaternion.identity);
                }
            }
        }

        private static void BuildCover(Transform parent, GameObject coverPrefab)
        {
            Transform group = Group("Cover", parent);

            // On the plaza, angled so they break line of sight diagonally across the centre.
            // Y is derived from the plaza top so the blocks stay seated if the steps change.
            float plazaCoverY = PlazaTopHeight + 1f;
            Vector3[] plazaCover =
            {
                new Vector3(-5f, plazaCoverY, -5f),
                new Vector3(5f, plazaCoverY, -5f),
                new Vector3(-5f, plazaCoverY, 5f),
                new Vector3(5f, plazaCoverY, 5f)
            };

            for (int index = 0; index < plazaCover.Length; index++)
            {
                Instantiate(coverPrefab, "Cover_Plaza_" + (index + 1), group, plazaCover[index], Quaternion.Euler(0f, 45f, 0f));
            }

            // Ground-level cover on the east/west approaches.
            Vector3[] groundCover =
            {
                new Vector3(-12f, 1f, 0f),
                new Vector3(12f, 1f, 0f),
                new Vector3(-12f, 1f, 6f),
                new Vector3(12f, 1f, 6f),
                new Vector3(-12f, 1f, -6f),
                new Vector3(12f, 1f, -6f)
            };

            for (int index = 0; index < groundCover.Length; index++)
            {
                Instantiate(coverPrefab, "Cover_Ground_" + (index + 1), group, groundCover[index], Quaternion.Euler(0f, 90f, 0f));
            }
        }

        /// <summary>Flat team-coloured decals under each spawn. Colliders removed so nothing snags.</summary>
        private static void BuildSpawnMarkers(Transform parent)
        {
            Transform group = Group("SpawnMarkers", parent);

            Decoration("Marker_TeamA_1", group, new Vector3(-6f, .03f, 24f), new Vector3(4f, .06f, 4f), teamAMaterial);
            Decoration("Marker_TeamA_2", group, new Vector3(6f, .03f, 24f), new Vector3(4f, .06f, 4f), teamAMaterial);
            Decoration("Marker_TeamB_1", group, new Vector3(-6f, .03f, -24f), new Vector3(4f, .06f, 4f), teamBMaterial);
            Decoration("Marker_TeamB_2", group, new Vector3(6f, .03f, -24f), new Vector3(4f, .06f, 4f), teamBMaterial);
        }

        /// <summary>
        /// Bare transforms only. The gameplay branch owns ArenaSpawnPoint and any spawn logic;
        /// these exist so that component has something to be dropped onto after integration.
        /// </summary>
        private static void BuildGameplayHooks(Transform parent)
        {
            Empty("ArenaCenter", parent, Vector3.zero, Quaternion.identity);

            // Spawns sit behind their tower so opponents cannot see each other at match start.
            // Rotations face each pair toward the arena centre.
            Empty("TeamA_Spawn_1", parent, new Vector3(-6f, .1f, 24f), Quaternion.Euler(0f, 180f, 0f));
            Empty("TeamA_Spawn_2", parent, new Vector3(6f, .1f, 24f), Quaternion.Euler(0f, 180f, 0f));
            Empty("TeamB_Spawn_1", parent, new Vector3(-6f, .1f, -24f), Quaternion.identity);
            Empty("TeamB_Spawn_2", parent, new Vector3(6f, .1f, -24f), Quaternion.identity);

            Empty("SpectatorCamera_Ref", parent, new Vector3(0f, 22f, -38f), Quaternion.Euler(30f, 0f, 0f));
        }

        /// <summary>
        /// One shadow-casting directional key light. Deliberately minimal: extra realtime lights
        /// are the easiest way to lose the frame budget on Quest 2, and graybox only needs the
        /// elevation changes to read clearly.
        /// </summary>
        private static void BuildLighting(Transform parent)
        {
            GameObject keyObject = new GameObject("KeyLight");
            keyObject.transform.SetParent(parent, false);
            keyObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, .96f, .89f);
            key.intensity = 1.15f;
            key.shadows = LightShadows.Soft;
        }

        /// <summary>Scene-local render settings only; project-wide graphics config is untouched.</summary>
        private static void ConfigureSceneRendering()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.46f, .52f, .60f);
            RenderSettings.ambientEquatorColor = new Color(.36f, .38f, .42f);
            RenderSettings.ambientGroundColor = new Color(.18f, .18f, .20f);
            RenderSettings.fog = false;
        }

        // ---------------------------------------------------------------------------------
        // Prefabs
        // ---------------------------------------------------------------------------------

        private static GameObject CreateCoverPrefab()
        {
            GameObject temp = Primitive("Env_CoverBlock", null, Vector3.zero, new Vector3(3f, 2f, 1.5f), coverMaterial);
            return SavePrefab(temp, "Env_CoverBlock");
        }

        private static GameObject CreatePillarPrefab()
        {
            GameObject temp = Primitive("Env_Pillar", null, Vector3.zero, new Vector3(1.8f, 10f, 1.8f), pillarMaterial);
            return SavePrefab(temp, "Env_Pillar");
        }

        private static GameObject CreatePlatformPrefab()
        {
            GameObject temp = Primitive("Env_PlatformDeck", null, Vector3.zero, new Vector3(6f, .6f, 6f), platformMaterial);
            return SavePrefab(temp, "Env_PlatformDeck");
        }

        private static GameObject CreateWallPrefab()
        {
            GameObject temp = Primitive("Env_WallSegment", null, Vector3.zero,
                new Vector3(PlayableHalfExtent * 2f + 2f, WallHeight, WallThickness), wallMaterial);
            return SavePrefab(temp, "Env_WallSegment");
        }

        /// <summary>
        /// Ramp root is axis-aligned with the slope baked into a child, so instances only need a
        /// position and a yaw. Rises <see cref="DeckHeight"/> over a 9m run (about 23 degrees).
        /// </summary>
        private static GameObject CreateRampPrefab()
        {
            GameObject root = new GameObject("Env_Ramp");

            const float run = 9f;
            float angle = Mathf.Atan2(DeckHeight, run) * Mathf.Rad2Deg;
            float slabLength = Mathf.Sqrt(run * run + DeckHeight * DeckHeight);
            const float thickness = .5f;

            // Offset the slab down along its own up axis so the walking surface, not the box
            // centre, is what meets the ground and the deck edge.
            float radians = angle * Mathf.Deg2Rad;
            Vector3 localUp = new Vector3(0f, Mathf.Cos(radians), -Mathf.Sin(radians));
            Vector3 slabCenter = new Vector3(0f, DeckHeight * .5f, 0f) - localUp * (thickness * .5f);

            GameObject slab = Primitive("Slab", root.transform, slabCenter,
                new Vector3(5f, thickness, slabLength), rampMaterial);
            slab.transform.localRotation = Quaternion.Euler(-angle, 0f, 0f);

            return SavePrefab(root, "Env_Ramp");
        }

        private static GameObject SavePrefab(GameObject temp, string prefabName)
        {
            EnsureFolder(PrefabsFolder);
            string path = PrefabsFolder + "/" + prefabName + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        // ---------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------

        private static Transform Group(string groupName, Transform parent)
        {
            GameObject group = new GameObject(groupName);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject Empty(string objectName, Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject empty = new GameObject(objectName);
            empty.transform.SetParent(parent, false);
            empty.transform.localPosition = position;
            empty.transform.localRotation = rotation;
            return empty;
        }

        private static GameObject Instantiate(
            GameObject prefab,
            string instanceName,
            Transform parent,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = instanceName;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            return instance;
        }

        private static GameObject Box(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            return Primitive(objectName, parent, position, scale, material);
        }

        private static GameObject Decoration(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject decoration = Primitive(objectName, parent, position, scale, material);
            Object.DestroyImmediate(decoration.GetComponent<Collider>());
            return decoration;
        }

        private static GameObject Primitive(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = objectName;

            if (parent != null)
                box.transform.SetParent(parent, false);

            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static void MarkStaticRecursive(GameObject root)
        {
            // Deliberately no NavigationStatic: it is deprecated in the AI Navigation package,
            // which selects bake geometry through NavMeshSurface collection instead. See
            // ArenaNavMeshBaker, which scopes the bake to this Environment hierarchy.
            const StaticEditorFlags flags = StaticEditorFlags.BatchingStatic
                                            | StaticEditorFlags.OccluderStatic
                                            | StaticEditorFlags.OccludeeStatic
                                            | StaticEditorFlags.ContributeGI;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
            }
        }

        // ---------------------------------------------------------------------------------
        // Materials
        // ---------------------------------------------------------------------------------

        private static void CreateMaterials()
        {
            // Value contrast carries the readability here: higher tier reads lighter, and ramps
            // get their own hue so a traversable slope never reads as a wall.
            groundMaterial = MakeMaterial("Env_Ground", new Color(.34f, .35f, .37f));
            plazaMaterial = MakeMaterial("Env_Plaza", new Color(.44f, .45f, .47f));
            wallMaterial = MakeMaterial("Env_Wall", new Color(.24f, .25f, .28f));
            platformMaterial = MakeMaterial("Env_Platform", new Color(.58f, .59f, .61f));
            rampMaterial = MakeMaterial("Env_Ramp", new Color(.60f, .49f, .33f));
            pillarMaterial = MakeMaterial("Env_Pillar", new Color(.29f, .30f, .33f));
            coverMaterial = MakeMaterial("Env_Cover", new Color(.49f, .50f, .52f));
            teamAMaterial = MakeMaterial("Env_TeamA", new Color(.28f, .44f, .70f));
            teamBMaterial = MakeMaterial("Env_TeamB", new Color(.70f, .34f, .30f));

            AssetDatabase.SaveAssets();
        }

        private static Material MakeMaterial(string materialName, Color color)
        {
            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + materialName + ".mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", .18f);
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
