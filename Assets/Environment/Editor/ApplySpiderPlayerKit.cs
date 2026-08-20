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
                Transform leftHand = FindDeep(root.transform, "LeftPhysicsHand");
                Transform rightHand = FindDeep(root.transform, "RightPhysicsHand");
                Transform camera = FindDeep(root.transform, "Main Camera");

                int recoloured = 0;
                recoloured += Recolour(leftHand, glove);
                recoloured += Recolour(rightHand, glove);

                WebSwingLocomotion swing = root.GetComponent<WebSwingLocomotion>();
                if (swing == null)
                    swing = root.AddComponent<WebSwingLocomotion>();

                // Private serialized fields, so assign through SerializedObject.
                SerializedObject serialized = new SerializedObject(swing);
                AssignReference(serialized, "leftHand", leftHand);
                AssignReference(serialized, "rightHand", rightHand);
                AssignReference(serialized, "head", camera);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);

                Debug.Log("[ApplySpiderPlayerKit] Recoloured " + recoloured +
                          " hand renderer(s) and wired WebSwingLocomotion. " +
                          "left=" + Name(leftHand) + " right=" + Name(rightHand) +
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
