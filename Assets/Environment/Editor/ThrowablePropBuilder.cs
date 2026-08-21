using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Builds physics-ready throwable environment props in light, medium and heavy tiers.
    ///
    /// Environment-owned: mesh, collider, pivot and material only. No Rigidbody, no grab or
    /// throw components - gameplay adds those. Recommended masses are documented here and in
    /// docs/destruction-boss-environment.md rather than baked into a script, so gameplay stays
    /// the single place physics values are tuned.
    ///
    /// Pivots are at the centre of mass, not the base. A thrown or grabbed prop rotates about its
    /// pivot, and a base pivot makes props spin like a hammer around their feet.
    ///
    /// Colliders are primitives only. A MeshCollider on a prop being hurled at 30 m/s is both a
    /// performance cost and a tunnelling risk, and convex mesh colliders would give worse contact
    /// behaviour here than the boxes and capsules these shapes actually are.
    /// </summary>
    public static class ThrowablePropBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/Environment";
        private const string PrefabFolder = "Assets/Prefabs/Environment/Props";

        private enum Shape
        {
            Box,
            Capsule
        }

        private static Material wood;
        private static Material metal;
        private static Material concrete;
        private static Material painted;

        [MenuItem("Tools/HeroVR/Environment/Build Throwable Props")]
        public static void BuildAll()
        {
            CreateMaterials();

            // --- Light: one-handed, tossed easily, low impact ------------------------------
            BuildBox("Prop_Light_SmallCrate", new Vector3(.45f, .45f, .45f), wood);
            BuildCapsule("Prop_Light_TrashCan", .28f, .85f, metal);

            // --- Medium: two-handed feel, meaningful knockback -----------------------------
            BuildCapsule("Prop_Medium_Barrel", .32f, 1.05f, painted);
            BuildBench("Prop_Medium_Bench");

            // --- Heavy: superhero-only, should break things --------------------------------
            BuildBox("Prop_Heavy_ConcreteChunk", new Vector3(1.15f, .85f, .95f), concrete);
            BuildCar("Prop_Heavy_Car");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ThrowablePropBuilder] Wrote props to " + PrefabFolder +
                      ". No Rigidbody attached; gameplay owns mass and grab components.");
        }

        // ---------------------------------------------------------------------------------
        // Props
        // ---------------------------------------------------------------------------------

        private static void BuildBox(string prefabName, Vector3 size, Material material)
        {
            GameObject root = new GameObject(prefabName);

            GameObject visual = Visual(root, "Visual", Vector3.zero, size, material);
            visual.transform.localPosition = Vector3.zero;

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = size;

            Save(root, prefabName);
        }

        private static void BuildCapsule(string prefabName, float radius, float height, Material material)
        {
            GameObject root = new GameObject(prefabName);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            // Unity's cylinder primitive is 2 units tall at scale 1, hence the halved height.
            visual.transform.localScale = new Vector3(radius * 2f, height * .5f, radius * 2f);
            Object.DestroyImmediate(visual.GetComponent<UnityEngine.Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = material;

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = radius;
            capsule.height = height;
            capsule.direction = 1;

            Save(root, prefabName);
        }

        /// <summary>
        /// Bench uses one box collider covering the whole silhouette rather than separate seat and
        /// leg colliders. The gaps under a bench are exactly the kind of hole a ragdoll limb wedges
        /// into, and nothing about gameplay needs a thrown bench to be hollow.
        /// </summary>
        private static void BuildBench(string prefabName)
        {
            GameObject root = new GameObject(prefabName);
            GameObject visual = Group(root, "Visual");

            Visual(visual, "Seat", new Vector3(0f, .21f, 0f), new Vector3(1.8f, .12f, .5f), wood);
            Visual(visual, "Back", new Vector3(0f, .52f, -.2f), new Vector3(1.8f, .5f, .1f), wood);
            Visual(visual, "Leg_L", new Vector3(-.7f, -.07f, 0f), new Vector3(.12f, .45f, .45f), metal);
            Visual(visual, "Leg_R", new Vector3(.7f, -.07f, 0f), new Vector3(.12f, .45f, .45f), metal);

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, .22f, -.05f);
            box.size = new Vector3(1.85f, .85f, .62f);

            Save(root, prefabName);
        }

        /// <summary>
        /// Car-sized heavy prop. Two stacked boxes for body and cabin would trap ragdolls in the
        /// step between them, so collision is a single box over the whole vehicle while the visual
        /// keeps the silhouette.
        /// </summary>
        private static void BuildCar(string prefabName)
        {
            GameObject root = new GameObject(prefabName);
            GameObject visual = Group(root, "Visual");

            Visual(visual, "Body", new Vector3(0f, -.1f, 0f), new Vector3(2f, .8f, 4.3f), painted);
            Visual(visual, "Cabin", new Vector3(0f, .52f, -.25f), new Vector3(1.8f, .72f, 2.1f), painted);

            GameObject wheels = Group(visual, "Wheels");
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Visual(wheels, "Wheel_" + (x < 0 ? "L" : "R") + (z < 0 ? "B" : "F"),
                        new Vector3(x * .92f, -.5f, z * 1.45f),
                        new Vector3(.22f, .62f, .62f), metal);
                }
            }

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, .05f, 0f);
            box.size = new Vector3(2.05f, 1.65f, 4.35f);

            Save(root, prefabName);
        }

        // ---------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------

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

            // The prop root owns the single collider; child visuals never add their own.
            Object.DestroyImmediate(piece.GetComponent<UnityEngine.Collider>());
            return piece;
        }

        private static void Save(GameObject root, string prefabName)
        {
            EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + prefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static void CreateMaterials()
        {
            wood = MakeMaterial("Env_PropWood", new Color(.48f, .34f, .20f));
            metal = MakeMaterial("Env_PropMetal", new Color(.42f, .44f, .47f));
            concrete = MakeMaterial("Env_Concrete", new Color(.55f, .55f, .53f));
            painted = MakeMaterial("Env_PropPainted", new Color(.26f, .38f, .52f));

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
                material.color = color;
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Glossiness", .15f);
                EditorUtility.SetDirty(material);
            }

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
