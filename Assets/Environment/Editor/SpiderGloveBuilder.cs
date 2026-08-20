using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Builds left and right wall-crawler gloves posed in the web-shooting gesture.
    ///
    /// Modelled on how this reads in other VR web-swinging games: index and pinky extended,
    /// middle and ring curled into the palm, a heavy grey cuff at the wrist, and a shooter on the
    /// underside of the wrist with a nozzle pointing along +Z.
    ///
    /// The nozzle matters mechanically, not just decoratively. Webs fire along the controller's
    /// forward axis, so a visible part of the glove pointing down that exact axis lets the player
    /// see where a shot will go before firing - which flat fingers-forward hands did not convey.
    ///
    /// Built at real hand size and mounted on the tracked controller transform, not the physics
    /// hand: the physics hand is scaled 0.18 and trails the true pose.
    ///
    /// Visual only, no colliders. The physics hand keeps its collider and PunchHitbox.
    /// </summary>
    public static class SpiderGloveBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/Environment";
        private const string HeroesFolder = "Assets/Environment/Heroes";

        private static Material suitRed;
        private static Material cuffGrey;
        private static Material detailBlack;

        [MenuItem("Tools/HeroVR/Environment/Build Spider Gloves")]
        public static void BuildGloves()
        {
            CreateMaterials();
            Build(isRight: false);
            Build(isRight: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SpiderGloveBuilder] Wrote Env_SpiderGlove_L / _R. " +
                      "Apply with Apply Spider Player Kit.");
        }

        private static void Build(bool isRight)
        {
            string suffix = isRight ? "R" : "L";

            // Mirror across X so thumb and finger spread read as the correct hand.
            float side = isRight ? 1f : -1f;

            GameObject root = new GameObject("Env_SpiderGlove_" + suffix);

            // --- Wrist -------------------------------------------------------------------
            // Heavy grey cuff, the strongest silhouette cue for which end is the wrist.
            Part("Cuff", root, new Vector3(0f, 0f, -.085f), new Vector3(.082f, .082f, .10f), cuffGrey);
            Part("CuffLip", root, new Vector3(0f, 0f, -.032f), new Vector3(.09f, .09f, .022f), cuffGrey);

            // Web shooter under the wrist, with a nozzle down +Z showing the firing direction.
            Part("Shooter", root, new Vector3(0f, -.03f, -.005f), new Vector3(.04f, .026f, .055f), detailBlack);
            Part("ShooterNozzle", root, new Vector3(0f, -.028f, .04f), new Vector3(.018f, .018f, .03f), detailBlack);

            // --- Palm --------------------------------------------------------------------
            Part("Palm", root, new Vector3(0f, 0f, .022f), new Vector3(.086f, .042f, .092f), suitRed);

            // Web line detail across the back of the hand.
            Part("WebLine_A", root, new Vector3(0f, .023f, .012f), new Vector3(.078f, .004f, .005f), detailBlack);
            Part("WebLine_B", root, new Vector3(0f, .023f, .046f), new Vector3(.066f, .004f, .005f), detailBlack);
            Part("WebLine_C", root, new Vector3(0f, .023f, .03f), new Vector3(.005f, .004f, .08f), detailBlack);

            // --- Extended fingers: index and pinky ---------------------------------------
            GameObject index = Part("Finger_Index", root,
                new Vector3(side * -.028f, .006f, .105f), new Vector3(.02f, .021f, .08f), suitRed);
            index.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);

            GameObject pinky = Part("Finger_Pinky", root,
                new Vector3(side * .034f, -.002f, .092f), new Vector3(.017f, .018f, .062f), suitRed);
            pinky.transform.localRotation = Quaternion.Euler(-4f, side * 9f, 0f);

            // --- Curled fingers: middle and ring -----------------------------------------
            // Tucked toward the palm, which is what makes the gesture read as "firing" rather
            // than "pointing".
            GameObject middle = Part("Finger_Middle", root,
                new Vector3(side * -.009f, -.026f, .062f), new Vector3(.02f, .048f, .024f), suitRed);
            middle.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

            GameObject ring = Part("Finger_Ring", root,
                new Vector3(side * .013f, -.026f, .058f), new Vector3(.019f, .044f, .023f), suitRed);
            ring.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

            // --- Thumb -------------------------------------------------------------------
            GameObject thumb = Part("Thumb", root,
                new Vector3(side * -.05f, -.008f, .03f), new Vector3(.022f, .025f, .05f), suitRed);
            thumb.transform.localRotation = Quaternion.Euler(0f, side * -34f, side * -12f);

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

            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        private static void CreateMaterials()
        {
            suitRed = Load("Hero_SpiderRed", new Color(.72f, .11f, .13f));
            cuffGrey = Load("Hero_SpiderCuff", new Color(.30f, .31f, .34f));
            detailBlack = Load("Hero_SpiderEmblem", new Color(.05f, .05f, .07f));
        }

        private static Material Load(string materialName, Color color)
        {
            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + materialName + ".mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
                material.color = color;
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Glossiness", .2f);
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
