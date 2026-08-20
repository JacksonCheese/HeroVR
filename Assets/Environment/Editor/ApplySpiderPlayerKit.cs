using HeroVR.Experimental;
using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Dresses XRPlayer as the wall-crawler: red gloves, and the experimental web-swing component
    /// wired to the tracked hands.
    ///
    /// The gloves matter more than the body model. In VR the player's own body is barely in view,
    /// but the hands are on screen constantly, so recolouring them is what actually makes the
    /// player feel like the hero.
    ///
    /// Re-runnable: existing swing components are reused rather than duplicated.
    /// </summary>
    public static class ApplySpiderPlayerKit
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Characters/XRPlayer.prefab";
        private const string GloveMaterialPath = "Assets/Materials/Environment/Hero_SpiderRed.mat";

        [MenuItem("Tools/HeroVR/Environment/Apply Spider Player Kit")]
        public static void Apply()
        {
            Material glove = AssetDatabase.LoadAssetAtPath<Material>(GloveMaterialPath);
            if (glove == null)
            {
                Debug.LogError("[ApplySpiderPlayerKit] Missing " + GloveMaterialPath +
                               ". Run Build Spider Hero Model first.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == null)
            {
                Debug.LogError("[ApplySpiderPlayerKit] Could not open " + PlayerPrefabPath);
                return;
            }

            try
            {
                // Physics hands are what the player sees, so the web is drawn from them.
                Transform leftHand = FindDeep(root.transform, "LeftPhysicsHand");
                Transform rightHand = FindDeep(root.transform, "RightPhysicsHand");

                // Aiming uses the tracked controllers instead. The physics hands lag the real pose
                // and rotate under physics, so webs fired from them went somewhere other than
                // where the player was pointing.
                Transform leftAim = FindDeep(root.transform, "LeftController");
                Transform rightAim = FindDeep(root.transform, "RightController");

                Transform camera = FindDeep(root.transform, "Main Camera");

                int recoloured = 0;
                recoloured += Recolour(leftHand, glove);
                recoloured += Recolour(rightHand, glove);

                WebSwingLocomotion swing = root.GetComponent<WebSwingLocomotion>();
                if (swing == null)
                    swing = root.AddComponent<WebSwingLocomotion>();

                // Private serialized fields, so assign through SerializedObject.
                SerializedObject serialized = new SerializedObject(swing);
                AssignReference(serialized, "leftAim", leftAim);
                AssignReference(serialized, "rightAim", rightAim);
                AssignReference(serialized, "leftHand", leftHand);
                AssignReference(serialized, "rightHand", rightHand);
                AssignReference(serialized, "head", camera);

                // Values already serialized on the prefab win over changed script defaults, so the
                // tuning has to be written explicitly or old values silently persist. This is the
                // one place to retune the swing.
                AssignFloat(serialized, "maxWebRange", 35f);
                AssignFloat(serialized, "aimAssistRadius", .18f);

                // 0 = ControllerForward, 1 = HeadThroughHand. Switch to 1 if aiming still feels
                // wrong; it ignores controller orientation entirely.
                AssignEnum(serialized, "aimMode", 0);

                // A Touch controller's forward runs down the handle, well below where the hand
                // looks like it points, so the ray needs tilting up.
                AssignFloat(serialized, "aimPitchOffset", -35f);

                // Lighter than real gravity so falls hang longer and arcs read as superhero
                // rather than realistic. Reel-in is raised to keep swings from going limp now
                // that there is less energy in the pendulum.
                AssignFloat(serialized, "gravity", -13f);
                AssignFloat(serialized, "reelInSpeed", 5.2f);

                AssignFloat(serialized, "minRopeLength", 2.5f);
                AssignFloat(serialized, "airControl", 5f);
                AssignFloat(serialized, "releaseBoost", 3.5f);
                AssignFloat(serialized, "attachSpeedCarry", 5f);
                AssignFloat(serialized, "maxSpeed", 30f);
                AssignFloat(serialized, "missVisualDuration", .18f);

                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);

                Debug.Log("[ApplySpiderPlayerKit] Recoloured " + recoloured +
                          " hand renderer(s) and wired WebSwingLocomotion. " +
                          "aim=" + Name(leftAim) + "/" + Name(rightAim) +
                          " draw=" + Name(leftHand) + "/" + Name(rightHand) +
                          " head=" + Name(camera));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int Recolour(Transform hand, Material material)
        {
            if (hand == null)
                return 0;

            int count = 0;
            foreach (Renderer renderer in hand.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
                count++;
            }

            return count;
        }

        private static void AssignEnum(SerializedObject serialized, string field, int value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[ApplySpiderPlayerKit] No serialized field named " + field);
                return;
            }

            property.enumValueIndex = value;
        }

        private static void AssignFloat(SerializedObject serialized, string field, float value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[ApplySpiderPlayerKit] No serialized field named " + field);
                return;
            }

            property.floatValue = value;
        }

        private static void AssignReference(SerializedObject serialized, string field, Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[ApplySpiderPlayerKit] No serialized field named " + field);
                return;
            }

            property.objectReferenceValue = value;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static string Name(Transform transform)
        {
            return transform != null ? transform.name : "<missing>";
        }
    }
}
