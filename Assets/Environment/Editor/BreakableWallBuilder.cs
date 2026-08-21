using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Builds authored breakable wall modules with intact, damaged and broken states.
    ///
    /// Destruction is authored, never runtime-fractured. Runtime fracture is the usual reason this
    /// kind of feature dies on Quest: it spikes on impact and produces unpredictable collider
    /// counts. Three pre-built states cost nothing at runtime beyond toggling GameObjects.
    ///
    /// Every state owns its own collider, and only one state is ever active, so nothing invisible
    /// is left behind after a break. The broken state's collider is deliberately built from two
    /// side pieces and a lintel rather than one box with a hole, because a box collider cannot
    /// have a hole: leaving a single box there is exactly how a "broken" wall ends up still
    /// blocking the passage it just opened.
    ///
    /// Environment-owned: geometry, colliders and materials only. No gameplay components. The
    /// state roots are named and ordered so a DestructibleStructure can be dropped on the root and
    /// have its serialized references assigned without searching for arbitrary child names.
    /// </summary>
    public static class BreakableWallBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/Environment";
        private const string PrefabFolder = "Assets/Prefabs/Environment/Breakable";

        // A wall thin enough to look right but thick enough that a thrown enemy or hammer at
        // 30 m/s does not tunnel through it between physics ticks.
        private const float ConcreteThickness = .55f;
        private const float BrickThickness = .45f;
        private const float InteriorThickness = .28f;

        private const float WallWidth = 8f;
        private const float WallHeight = 5f;

        // The opening a broken wall leaves. Sized so a flying player, a ragdolling enemy, or a
        // boss-thrown prop all pass through comfortably.
        private const float OpeningWidth = 4.2f;
        private const float OpeningHeight = 3.4f;

        private static Material concrete;
        private static Material concreteDamaged;
        private static Material brick;
        private static Material brickDamaged;
        private static Material interior;
        private static Material interiorDamaged;
        private static Material rubble;

        [MenuItem("Tools/HeroVR/Environment/Build Breakable Walls")]
        public static void BuildAll()
        {
            CreateMaterials();

            Build("Wall_Breakable_Concrete", ConcreteThickness, concrete, concreteDamaged, 4);
            Build("Wall_Breakable_Brick", BrickThickness, brick, brickDamaged, 5);
            Build("Wall_Breakable_Interior", InteriorThickness, interior, interiorDamaged, 3);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BreakableWallBuilder] Wrote breakable wall prefabs to " + PrefabFolder +
                      ". Only IntactState is active; gameplay toggles the others.");
        }

        private static void Build(
            string prefabName,
            float thickness,
            Material intactMaterial,
            Material damagedMaterial,
            int debrisCount)
        {
            GameObject root = new GameObject(prefabName);

            BuildIntact(root, thickness, intactMaterial);
            BuildDamaged(root, thickness, damagedMaterial);
            BuildBroken(root, thickness, damagedMaterial, debrisCount);

            EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + prefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------------------------
        // States
        // ---------------------------------------------------------------------------------

        private static void BuildIntact(GameObject root, float thickness, Material material)
        {
            GameObject state = State(root, "IntactState", active: true);

            Visual(state, "Visual", new Vector3(0f, WallHeight * .5f, 0f),
                new Vector3(WallWidth, WallHeight, thickness), material);

            Collider(state, "Collider", new Vector3(0f, WallHeight * .5f, 0f),
                new Vector3(WallWidth, WallHeight, thickness));
        }

        /// <summary>
        /// Still solid, but visibly stressed. Collision is unchanged from intact on purpose: a
        /// damaged wall the player can already walk through would make the broken state pointless.
        /// </summary>
        private static void BuildDamaged(GameObject root, float thickness, Material material)
        {
            GameObject state = State(root, "DamagedState", active: false);

            Visual(state, "Visual", new Vector3(0f, WallHeight * .5f, 0f),
                new Vector3(WallWidth, WallHeight, thickness), material);

            // Displaced blocks around where the wall will eventually give way. Cheap way to read
            // as "about to break" without a second material or any transparency.
            GameObject cracks = Group(state, "CrackDetail");
            float openingBase = OpeningHeight * .5f;

            Visual(cracks, "Crack_A", new Vector3(-.9f, openingBase + .7f, thickness * .35f),
                new Vector3(1.5f, .28f, thickness * .5f), material)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 9f);

            Visual(cracks, "Crack_B", new Vector3(1.1f, openingBase - .3f, thickness * .35f),
                new Vector3(1.2f, .24f, thickness * .5f), material)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -13f);

            Visual(cracks, "Crack_C", new Vector3(.1f, openingBase + 1.4f, -thickness * .35f),
                new Vector3(1.7f, .22f, thickness * .5f), material)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 5f);

            Collider(state, "Collider", new Vector3(0f, WallHeight * .5f, 0f),
                new Vector3(WallWidth, WallHeight, thickness));
        }

        /// <summary>
        /// The opening is the whole point of this module, so both visual and collision are built
        /// as three pieces around a genuine hole rather than a single box.
        /// </summary>
        private static void BuildBroken(
            GameObject root,
            float thickness,
            Material material,
            int debrisCount)
        {
            GameObject state = State(root, "BrokenState", active: false);
            GameObject visual = Group(state, "Visual");
            GameObject collision = Group(state, "Collider");

            float sideWidth = (WallWidth - OpeningWidth) * .5f;
            float sideCentre = (OpeningWidth + sideWidth) * .5f;
            float lintelHeight = WallHeight - OpeningHeight;
            float lintelCentre = OpeningHeight + lintelHeight * .5f;

            // Left and right remnants.
            for (int side = -1; side <= 1; side += 2)
            {
                string suffix = side < 0 ? "L" : "R";
                Vector3 position = new Vector3(side * sideCentre, WallHeight * .5f, 0f);
                Vector3 size = new Vector3(sideWidth, WallHeight, thickness);

                Visual(visual, "Side_" + suffix, position, size, material);
                Collider(collision, "Side_" + suffix, position, size);
            }

            // Lintel spanning the opening.
            Vector3 lintelPosition = new Vector3(0f, lintelCentre, 0f);
            Vector3 lintelSize = new Vector3(OpeningWidth, lintelHeight, thickness);
            Visual(visual, "Lintel", lintelPosition, lintelSize, material);
            Collider(collision, "Lintel", lintelPosition, lintelSize);

            // Ragged edges around the opening. Visual only - no colliders, so they cannot snag a
            // ragdoll or a flying player passing through the gap.
            GameObject edges = Group(visual, "BrokenEdges");
            Visual(edges, "Edge_L", new Vector3(-OpeningWidth * .5f + .18f, OpeningHeight * .55f, 0f),
                new Vector3(.36f, 1.1f, thickness * .92f), material)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
            Visual(edges, "Edge_R", new Vector3(OpeningWidth * .5f - .22f, OpeningHeight * .4f, 0f),
                new Vector3(.44f, .9f, thickness * .92f), material)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            Visual(edges, "Edge_Top", new Vector3(.4f, OpeningHeight - .12f, 0f),
                new Vector3(1.3f, .3f, thickness * .92f), material)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -4f);

            BuildDebris(state, thickness, debrisCount);
        }

        /// <summary>
        /// A few large chunks rather than many small fragments. Dozens of rigidbody shards is the
        /// standard way destruction becomes unshippable on Quest; these are authored, inactive,
        /// and left without Rigidbodies so gameplay decides whether they ever simulate.
        /// </summary>
        private static void BuildDebris(GameObject state, float thickness, int count)
        {
            GameObject debris = Group(state, "Debris");

            for (int index = 0; index < count; index++)
            {
                float scale = .45f + (index % 3) * .18f;

                // Rubble is pushed out past the edges of the opening rather than piled in the
                // middle of it. Debris in the gap reads as realistic but physically blocks the
                // passage the break just created, which is the failure this module exists to
                // avoid - a thrown enemy would bounce off a pile of gravel.
                float side = index % 2 == 0 ? 1f : -1f;
                float x = side * (OpeningWidth * .5f + .35f + (index / 2) * .45f);
                float z = side * (thickness * .5f + .55f + (index % 3) * .25f);

                GameObject chunk = Visual(debris, "Chunk_" + (index + 1),
                    new Vector3(x, scale * .5f, z),
                    new Vector3(scale, scale * .75f, scale * .85f), rubble);

                chunk.transform.localRotation = Quaternion.Euler(
                    index * 17f % 30f, index * 41f % 360f, index * 13f % 25f);

                // Simple box collision so a chunk can be picked up or kicked without a
                // MeshCollider. Gameplay adds the Rigidbody if it wants these to simulate.
                BoxCollider box = chunk.AddComponent<BoxCollider>();
                box.size = Vector3.one;
            }
        }

        // ---------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------

        private static GameObject State(GameObject root, string name, bool active)
        {
            GameObject state = new GameObject(name);
            state.transform.SetParent(root.transform, false);
            state.SetActive(active);
            return state;
        }

        private static GameObject Group(GameObject parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent.transform, false);
            return group;
        }

        private static GameObject Visual(
            GameObject parent,
            string name,
            Vector3 position,
            Vector3 size,
            Material material)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent.transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = size;
            piece.GetComponent<Renderer>().sharedMaterial = material;

            // Visuals never carry collision; the state's Collider group owns all of it.
            Object.DestroyImmediate(piece.GetComponent<UnityEngine.Collider>());
            return piece;
        }

        /// <summary>Collision-only box: no renderer, so it costs nothing to draw.</summary>
        private static void Collider(GameObject parent, string name, Vector3 position, Vector3 size)
        {
            GameObject collision = new GameObject(name);
            collision.transform.SetParent(parent.transform, false);
            collision.transform.localPosition = position;

            BoxCollider box = collision.AddComponent<BoxCollider>();
            box.size = size;
        }

        private static void CreateMaterials()
        {
            concrete = MakeMaterial("Env_Concrete", new Color(.55f, .55f, .53f));
            concreteDamaged = MakeMaterial("Env_ConcreteDamaged", new Color(.44f, .43f, .41f));
            brick = MakeMaterial("Env_Brick", new Color(.52f, .29f, .23f));
            brickDamaged = MakeMaterial("Env_BrickDamaged", new Color(.41f, .23f, .19f));
            interior = MakeMaterial("Env_InteriorWall", new Color(.74f, .72f, .67f));
            interiorDamaged = MakeMaterial("Env_InteriorWallDamaged", new Color(.6f, .58f, .53f));
            rubble = MakeMaterial("Env_Rubble", new Color(.38f, .37f, .35f));

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
            material.SetFloat("_Glossiness", .1f);
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
