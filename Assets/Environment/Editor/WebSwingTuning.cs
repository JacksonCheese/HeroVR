using HeroVR.Experimental;
using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Quick tuning for web-swing aim and the aim ray.
    ///
    /// Aim pitch has no single correct value; it depends on how the player grips the controller.
    /// These exist so it can be bracketed by trying a few settings between headset sessions rather
    /// than guessed at from outside the headset, which has already gone wrong twice.
    /// </summary>
    public static class WebSwingTuning
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Characters/XRPlayer.prefab";

        // Positive aims DOWN. Bracketed downward because the controller's forward axis sits above
        // where the index finger points, so the useful range is on the down side of level.
        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - Level (0)")]
        public static void PitchLevel() => SetPitch(0f);

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - Down 10")]
        public static void PitchDown10() => SetPitch(10f);

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - Down 15 (default)")]
        public static void PitchDown15() => SetPitch(15f);

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - Down 22")]
        public static void PitchDown22() => SetPitch(22f);

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - Down 30")]
        public static void PitchDown30() => SetPitch(30f);

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - Up 10")]
        public static void PitchUp10() => SetPitch(-10f);

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Aim Ray - Show")]
        public static void ShowAimRay() => SetBool("showAimRay", true, "aim ray on");

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Aim Ray - Hide")]
        public static void HideAimRay() => SetBool("showAimRay", false, "aim ray off");

        private static void SetPitch(float degrees)
        {
            Edit(swing => SetFloat(swing, "aimPitchOffset", degrees),
                "aim pitch = " + degrees + " degrees");
        }

        private static void SetBool(string field, bool value, string description)
        {
            Edit(swing => SetBoolValue(swing, field, value), description);
        }

        private static void Edit(System.Action<WebSwingLocomotion> change, string description)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == null)
            {
                Debug.LogError("[WebSwingTuning] Could not open " + PlayerPrefabPath);
                return;
            }

            try
            {
                WebSwingLocomotion swing = root.GetComponent<WebSwingLocomotion>();
                if (swing == null)
                {
                    Debug.LogError("[WebSwingTuning] No WebSwingLocomotion on the player. " +
                                   "Run Apply Spider Player Kit first.");
                    return;
                }

                change(swing);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log("[WebSwingTuning] " + description);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetFloat(Object target, string field, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[WebSwingTuning] No field named " + field);
                return;
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolValue(Object target, string field, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[WebSwingTuning] No field named " + field);
                return;
            }

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
