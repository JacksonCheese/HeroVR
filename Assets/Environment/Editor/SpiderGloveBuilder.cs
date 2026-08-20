using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Builds left and right wall-crawler glove models.
    ///
    /// The point is orientation, not decoration. The placeholder hands were featureless blobs, so
    /// there was no way to read which way a hand was pointing - which made aiming webs guesswork.
    /// Extended fingers, an offset thumb, and a wrist shooter give the hand a clear front, back,
    /// and roll axis at a glance.
    ///
    /// Built at real hand size and meant to hang off the tracked controller transform, not the
    /// physics hand. The physics hand is scaled 0.18, which would shrink a real-size model, and
    /// it lags slightly behind the true pose; hanging the visual off the controller keeps what the
    /// player sees exactly aligned with where a web will actually fire.
    ///
    /// Visual only: no colliders. The physics hand keeps its collider and PunchHitbox.
    /// </summary>
    public static class SpiderGloveBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/Environment";
        private const string HeroesFolder = "Assets/Environment/Heroes";

        private static Material suitRed;
        private static Material suitBlue;
        private static Material detailBlack;

        [MenuItem("Tools/HeroVR/Environment/Build Spider Gloves")]
        public static void BuildGloves()
        {
            CreateMaterials();
            Build(isRight: false);
            Build(isRight: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SpiderGloveBuilder] Wrote Env_SpiderGlove_L / _R to " + HeroesFolder +
                      ". Apply with Apply Spider Player Kit.");
        }

        private static void Build(bool isRight)
        {
            string suffix = isRight ? "R" : "L";

            // Mirror the thumb across X so each glove reads as the correct hand.
            float mirror = isRight ? 1f : -1f;

            GameObject root = new GameObject("Env_SpiderGlove_" + suffix);

            // Cuff sits behind the palm, marking the wrist end so the hand has an obvious back.
            Part("Cuff", root, new Vector3(0f, 0f, -.05f), new Vector3(.085f, .075f, .05f), suitBlue);

            Part("Palm", root, new Vector3(0f, 0f, .012f), new Vector3(.088f, .042f, .10f), suitRed);

            // Web shooter on the underside of the wrist: a strong roll cue, and the detail that
            // makes the glove read as this character rather than a generic red hand.
            Part("WebShooter", root, new Vector3(0f, -.028f, -.012f),
                new Vector3(.042f, .022f, .055f), detailBlack);

            // Four fingers pointing along +Z. This is what makes aim direction readable.
            for (int index = 0; index < 4; index++)
            {
                float x = -.031f + index * .021f;
                float length = index == 0 || index == 3 ? .055f : .065f;

                Part("Finger_" + (index + 1), root,
                    new Vector3(x, .004f, .062f + length * .5f),
                    new Vector3(.018f, .021f, length), suitRed);
            }

            // Thumb angled out to the side, breaking the left/right symmetry.
            GameObject thumb = Part("Thumb", root,
                new Vector3(mirror * .052f, -.004f, .022f),
                new Vector3(.021f, .024f, .052f), suitRed);
            thumb.transform.localRotation = Quaternion.Euler(0f, mirror * -32f, 0f);

            // Dark strip along the back of the hand: distinguishes palm from back of hand, which
            // is otherwise ambiguous on a symmetrical block.
            Part("BackStripe", root, new Vector3(0f, .023f, .012f),
                new Vector3(.03f, .006f, .085f), detailBlack);

            EnsureFolder(HeroesFolder);
            PrefabUtility.SaveAsPrefabAsset(root, HeroesFolder + "/Env_SpiderGlove_" + suffix + ".prefab");
            Object.DestroyImmediate(root);
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

            // Collision belongs to the physics hand, not the visual.
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        private static void CreateMaterials()
        {
            suitRed = Load("Hero_SpiderRed", new Color(.72f, .11f, .13f));
            suitBlue = Load("Hero_SpiderBlue", new Color(.09f, .15f, .44f));
            detailBlack = Load("Hero_SpiderEmblem", new Color(.05f, .05f, .07f));
        }

        private static Material Load(string materialName, Color fallbackColor)
        {
            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + materialName + ".mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Standard")) { color = fallbackColor };
            AssetDatabase.CreateAsset(material, path);
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
