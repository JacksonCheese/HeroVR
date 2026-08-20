using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Builds the visual-only wall-crawler hero body. Environment-owned: geometry and materials
    /// only, no gameplay code.
    ///
    /// Sized to the XRPlayer CharacterController (height 1.7, radius 0.3, centre y 0.85), which
    /// puts the origin at the FEET - unlike TrainingEnemy, whose capsule is centred on the origin
    /// at the waist. Do not copy proportions between the two.
    ///
    /// The build is slimmer and taller-shouldered than the thug so the two silhouettes stay
    /// distinguishable at distance, which matters more than detail once both are moving fast.
    ///
    /// The costume is read through colour blocking and the eye shapes rather than a web pattern.
    /// Webbing is fine line work that wants a texture; faking it with primitives would cost a lot
    /// of draw calls on Quest and still look wrong up close in VR.
    /// </summary>
    public static class SpiderHeroBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/Environment";
        private const string HeroesFolder = "Assets/Environment/Heroes";
        private const string PrefabPath = HeroesFolder + "/Env_SpiderHero.prefab";

        // XRPlayer CharacterController: height 1.7, radius 0.3, origin at the soles.
        private const float CapsuleRadius = .3f;
        private const float CapsuleHeight = 1.7f;

        private static Material suitRed;
        private static Material suitBlue;
        private static Material eyeWhite;
        private static Material emblemBlack;

        [MenuItem("Tools/HeroVR/Environment/Build Spider Hero Model")]
        public static void BuildSpiderHero()
        {
            CreateMaterials();

            GameObject root = new GameObject("Env_SpiderHero");

            // Chest first, matching the convention used for the enemy: any code that grabs the
            // first renderer to flash a hit or telegraph lands on the torso, not a boot.
            Part("Torso", root, new Vector3(0f, 1.14f, 0f), new Vector3(.34f, .36f, .19f), suitRed);
            Part("SpiderEmblem", root, new Vector3(0f, 1.17f, .1f), new Vector3(.15f, .17f, .02f), emblemBlack);

            Part("Shoulders", root, new Vector3(0f, 1.33f, 0f), new Vector3(.44f, .10f, .20f), suitRed);
            Part("Arm_L", root, new Vector3(-.23f, 1.12f, 0f), new Vector3(.09f, .38f, .10f), suitRed);
            Part("Arm_R", root, new Vector3(.23f, 1.12f, 0f), new Vector3(.09f, .38f, .10f), suitRed);
            Part("Glove_L", root, new Vector3(-.23f, .90f, 0f), new Vector3(.10f, .11f, .11f), suitRed);
            Part("Glove_R", root, new Vector3(.23f, .90f, 0f), new Vector3(.10f, .11f, .11f), suitRed);

            Part("Hips", root, new Vector3(0f, .91f, 0f), new Vector3(.30f, .14f, .17f), suitBlue);
            Part("UpperLeg_L", root, new Vector3(-.09f, .68f, 0f), new Vector3(.14f, .36f, .15f), suitBlue);
            Part("UpperLeg_R", root, new Vector3(.09f, .68f, 0f), new Vector3(.14f, .36f, .15f), suitBlue);
            Part("LowerLeg_L", root, new Vector3(-.09f, .30f, 0f), new Vector3(.12f, .40f, .13f), suitBlue);
            Part("LowerLeg_R", root, new Vector3(.09f, .30f, 0f), new Vector3(.12f, .40f, .13f), suitBlue);
            Part("Boot_L", root, new Vector3(-.09f, .05f, .02f), new Vector3(.13f, .10f, .24f), suitRed);
            Part("Boot_R", root, new Vector3(.09f, .05f, .02f), new Vector3(.13f, .10f, .24f), suitRed);

            Part("Neck", root, new Vector3(0f, 1.40f, 0f), new Vector3(.10f, .06f, .10f), suitRed);
            Part("Head", root, new Vector3(0f, 1.52f, 0f), new Vector3(.20f, .22f, .21f), suitRed);

            // Angled teardrop eyes are what actually sells the mask in silhouette.
            GameObject eyeLeft = Part("Eye_L", root, new Vector3(-.055f, 1.535f, .102f),
                new Vector3(.08f, .05f, .02f), eyeWhite);
            eyeLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);

            GameObject eyeRight = Part("Eye_R", root, new Vector3(.055f, 1.535f, .102f),
                new Vector3(.08f, .05f, .02f), eyeWhite);
            eyeRight.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);

            VerifyFitsCapsule(root);

            EnsureFolder(HeroesFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SpiderHeroBuilder] Wrote " + PrefabPath + " (" +
                      "visual only, no colliders). Parent it under XRPlayer at local zero.");
        }

        /// <summary>
        /// The player capsule is gameplay-owned and must not be resized, so the model has to fit
        /// inside it. Anything sticking out would clip through gaps the player cannot pass.
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
                    Debug.LogWarning("[SpiderHeroBuilder] " + renderer.name + " reaches " +
                                     horizontal.ToString("0.###") + "m from centre, outside the " +
                                     CapsuleRadius + "m capsule radius.");
                }

                if (bounds.min.y < -.001f || bounds.max.y > CapsuleHeight + .001f)
                {
                    Debug.LogWarning("[SpiderHeroBuilder] " + renderer.name + " spans y " +
                                     bounds.min.y.ToString("0.###") + " to " +
                                     bounds.max.y.ToString("0.###") +
                                     ", outside the 0 to " + CapsuleHeight + " capsule.");
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

            // The player's CharacterController is the only collision volume.
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        private static void CreateMaterials()
        {
            suitRed = MakeMaterial("Hero_SpiderRed", new Color(.72f, .11f, .13f));
            suitBlue = MakeMaterial("Hero_SpiderBlue", new Color(.09f, .15f, .44f));
            eyeWhite = MakeMaterial("Hero_SpiderEye", new Color(.93f, .94f, .96f));
            emblemBlack = MakeMaterial("Hero_SpiderEmblem", new Color(.05f, .05f, .07f));

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
            material.SetFloat("_Glossiness", .22f);
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
