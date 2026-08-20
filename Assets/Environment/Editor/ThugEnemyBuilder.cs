using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Builds the visual-only "street thug" body used to dress the training enemy, replacing the
    /// placeholder capsule. Environment-owned: geometry and materials only, no gameplay code.
    ///
    /// Three constraints come from the gameplay side and must be preserved:
    ///
    /// 1. TrainingEnemy's CapsuleCollider is centred on the origin (height 2, radius 0.5), so the
    ///    origin sits at the waist. The body is modelled from y -1 (soles) to y +1 (hat), and
    ///    stays inside radius 0.5, so the existing collider still fits without being touched.
    /// 2. TrainingBot recolours GetComponentInChildren&lt;Renderer&gt;() to telegraph an attack, which
    ///    takes the first renderer in hierarchy order. ChestPlate is created first on purpose so
    ///    the telegraph lands on a large, forward-facing panel.
    /// 3. The placeholder was red so the enemy reads instantly in a fight. That readability is
    ///    kept through the red chest plate, bandana, and shoulder flashes rather than colouring
    ///    the whole body, so it looks like a person but still reads as the enemy at a glance.
    ///
    /// Parts carry no colliders; the character's capsule remains the only collision volume.
    /// </summary>
    public static class ThugEnemyBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/Environment";
        private const string EnemiesFolder = "Assets/Environment/Enemies";
        private const string PrefabPath = EnemiesFolder + "/Env_ThugEnemy.prefab";

        // Keep every part inside the capsule so collision and visuals agree.
        private const float CapsuleRadius = .5f;
        private const float CapsuleHalfHeight = 1f;

        private static Material jacket;
        private static Material pants;
        private static Material boots;
        private static Material skin;
        private static Material accent;

        [MenuItem("Tools/HeroVR/Environment/Build Thug Enemy Model")]
        public static void BuildThugEnemy()
        {
            CreateMaterials();

            GameObject root = new GameObject("Env_ThugEnemy");

            // Created first so TrainingBot's GetComponentInChildren<Renderer>() picks it up as the
            // attack telegraph surface.
            Part("ChestPlate", root, new Vector3(0f, .25f, .04f), new Vector3(.40f, .40f, .36f), accent);

            Part("Torso", root, new Vector3(0f, .25f, 0f), new Vector3(.58f, .60f, .34f), jacket);

            // Wide, slightly raised shoulders read as a heavy-set brawler rather than a mannequin.
            Part("Shoulders", root, new Vector3(0f, .51f, 0f), new Vector3(.76f, .18f, .38f), jacket);
            Part("ShoulderFlash_L", root, new Vector3(-.32f, .51f, 0f), new Vector3(.14f, .19f, .39f), accent);
            Part("ShoulderFlash_R", root, new Vector3(.32f, .51f, 0f), new Vector3(.14f, .19f, .39f), accent);

            Part("Arm_L", root, new Vector3(-.40f, .22f, 0f), new Vector3(.17f, .55f, .19f), jacket);
            Part("Arm_R", root, new Vector3(.40f, .22f, 0f), new Vector3(.17f, .55f, .19f), jacket);
            Part("Fist_L", root, new Vector3(-.40f, -.09f, 0f), new Vector3(.20f, .14f, .22f), skin);
            Part("Fist_R", root, new Vector3(.40f, -.09f, 0f), new Vector3(.20f, .14f, .22f), skin);

            Part("Leg_L", root, new Vector3(-.15f, -.445f, 0f), new Vector3(.22f, .79f, .24f), pants);
            Part("Leg_R", root, new Vector3(.15f, -.445f, 0f), new Vector3(.22f, .79f, .24f), pants);
            Part("Boot_L", root, new Vector3(-.15f, -.92f, .02f), new Vector3(.24f, .16f, .34f), boots);
            Part("Boot_R", root, new Vector3(.15f, -.92f, .02f), new Vector3(.24f, .16f, .34f), boots);

            Part("Head", root, new Vector3(0f, .74f, 0f), new Vector3(.30f, .28f, .28f), skin);

            // Bandana over the lower face, proud of the head so it reads in silhouette.
            Part("Bandana", root, new Vector3(0f, .66f, .015f), new Vector3(.31f, .13f, .30f), accent);
            Part("Beanie", root, new Vector3(0f, .91f, 0f), new Vector3(.33f, .14f, .31f), boots);

            VerifyFitsCapsule(root);

            EnsureFolder(EnemiesFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ThugEnemyBuilder] Wrote " + PrefabPath +
                      ". Attach under TrainingEnemy and remove that object's own MeshFilter/" +
                      "MeshRenderer so the capsule mesh stops drawing.");
        }

        /// <summary>
        /// The capsule is gameplay-owned and must not be resized, so the model has to fit it.
        /// A part poking outside would visually clip through walls the character cannot enter.
        /// </summary>
        private static void VerifyFitsCapsule(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = renderer.bounds;

                float horizontal = Mathf.Max(
                    new Vector2(bounds.max.x, bounds.max.z).magnitude,
                    new Vector2(bounds.min.x, bounds.min.z).magnitude);

                if (horizontal > CapsuleRadius + .001f)
                {
                    Debug.LogWarning("[ThugEnemyBuilder] " + renderer.name + " reaches " +
                                     horizontal.ToString("0.###") + "m from centre, outside the " +
                                     CapsuleRadius + "m capsule radius.");
                }

                if (bounds.max.y > CapsuleHalfHeight + .001f || bounds.min.y < -CapsuleHalfHeight - .001f)
                {
                    Debug.LogWarning("[ThugEnemyBuilder] " + renderer.name + " spans y " +
                                     bounds.min.y.ToString("0.###") + " to " +
                                     bounds.max.y.ToString("0.###") + ", outside the capsule.");
                }
            }
        }

        private static GameObject Part(
            string partName,
            GameObject parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;

            // The character's capsule is the only collision volume; body parts must not add more.
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        private static void CreateMaterials()
        {
            jacket = MakeMaterial("Enemy_ThugJacket", new Color(.17f, .18f, .22f));
            pants = MakeMaterial("Enemy_ThugPants", new Color(.24f, .23f, .25f));
            boots = MakeMaterial("Enemy_ThugBoots", new Color(.10f, .10f, .12f));
            skin = MakeMaterial("Enemy_ThugSkin", new Color(.58f, .43f, .33f));
            accent = MakeMaterial("Enemy_ThugAccent", new Color(.70f, .16f, .15f));

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
