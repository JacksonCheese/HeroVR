using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace HeroVR.Editor
{
    public static class QuestOpenXRConfigurator
    {
        [MenuItem("HeroVR/Configure Quest OpenXR")]
        public static void Configure()
        {
            OpenXRSettings androidSettings =
                OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (androidSettings == null)
                throw new InvalidOperationException("Android OpenXR settings are missing.");

            OculusTouchControllerProfile touchProfile =
                androidSettings.GetFeature<OculusTouchControllerProfile>();
            MetaQuestFeature questSupport =
                androidSettings.GetFeature<MetaQuestFeature>();
            if (touchProfile == null || questSupport == null)
            {
                throw new InvalidOperationException(
                    "Required Quest OpenXR features are missing from Android settings.");
            }

            touchProfile.enabled = true;
            questSupport.enabled = true;
            EditorUtility.SetDirty(touchProfile);
            EditorUtility.SetDirty(questSupport);
            EditorUtility.SetDirty(androidSettings);
            AssetDatabase.SaveAssets();

            OpenXRSettings standaloneSettings =
                OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            OculusTouchControllerProfile standaloneTouch =
                standaloneSettings != null
                    ? standaloneSettings.GetFeature<OculusTouchControllerProfile>()
                    : null;

            Debug.Log(
                "Quest OpenXR configured: Android Oculus Touch Controller Profile and " +
                "Meta Quest Support enabled. Standalone Oculus Touch remains " +
                $"{(standaloneTouch != null && standaloneTouch.enabled ? "enabled" : "unchanged/disabled")}.");
        }
    }
}
