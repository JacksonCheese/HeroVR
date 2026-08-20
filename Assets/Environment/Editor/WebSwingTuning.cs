using HeroVR.Experimental;
using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Quick tuning for web-swing aim, so aim mode and pitch can be changed without hunting
    /// through the Inspector between headset sessions.
    ///
    /// Aim pitch has no single correct value: it depends on how the player grips the controller.
    /// These menu items exist so it can be bracketed quickly instead of guessed at.
    /// </summary>
    public static class WebSwingTuning
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Characters/XRPlayer.prefab";

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Mode - Head Through Hand")]
        public static void UseHeadThroughHand()
        {
            Edit(swing => SetEnum(swing, "aimMode", 1), "aim mode = HeadThroughHand");
        }

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Mode - Controller Pointing")]
        public static void UseControllerForward()
        {
            Edit(swing => SetEnum(swing, "aimMode", 0), "aim mode = ControllerForward");
        }

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - Level (0)")]
        public static void PitchLevel()
        {
            SetPitch(0f);
        }

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - Slightly Up (-12)")]
        public static void PitchSlight()
        {
            SetPitch(-12f);
        }

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - More Up (-22)")]
        public static void PitchMore()
        {
            SetPitch(-22f);
        }

        [MenuItem("Tools/HeroVR/Environment/Web Aim/Pitch - Aim Down (+12)")]
        public static void PitchDown()
        {
            SetPitch(12f);
        }

        private static void SetPitch(float degrees)
        {
            Edit(swing =>
            {
                SetEnum(swing, "aimMode", 0);
                SetFloat(swing, "aimPitchOffset", degrees);
            }, "aim mode = ControllerForward, pitch = " + degrees);
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

        private static void SetEnum(Object target, string field, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(field).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
