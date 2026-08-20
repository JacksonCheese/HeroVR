using System.Collections.Generic;
using HeroVR.Heroes;
using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Swaps which HeroDefinition the XR player uses, so heroes can be tried in VR without
    /// hand-editing the prefab.
    ///
    /// This is a stopgap for playtesting, not a hero-select feature. Choosing a hero at match time
    /// is gameplay work and belongs with the rest of the hero system.
    ///
    /// Note that HeroDefinition currently carries no body-model reference, so the player's visual
    /// cannot follow the selected hero automatically. Switching here changes stats and ability
    /// names only; the body has to be swapped separately.
    /// </summary>
    public static class PlayerHeroSwitcher
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Characters/XRPlayer.prefab";
        private const string DesktopPlayerPrefabPath = "Assets/Prefabs/Characters/DesktopPlayer.prefab";

        [MenuItem("Tools/HeroVR/Environment/Player Hero/Spider-Man")]
        public static void UseSpiderHero()
        {
            Apply("Assets/Heroes/SpiderHero/SpiderHero.asset");
        }

        [MenuItem("Tools/HeroVR/Environment/Player Hero/Kinetic Vanguard")]
        public static void UseKineticVanguard()
        {
            Apply("Assets/Heroes/KineticVanguard/KineticVanguard.asset");
        }

        [MenuItem("Tools/HeroVR/Environment/Player Hero/List Available Heroes")]
        public static void ListHeroes()
        {
            List<string> found = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:HeroDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                HeroDefinition definition = AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
                if (definition != null)
                    found.Add(definition.DisplayName + "  (" + path + ")");
            }

            Debug.Log("[PlayerHeroSwitcher] Hero definitions in project:\n  " + string.Join("\n  ", found));
        }

        private static void Apply(string heroAssetPath)
        {
            HeroDefinition hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(heroAssetPath);
            if (hero == null)
            {
                Debug.LogError("[PlayerHeroSwitcher] No HeroDefinition at " + heroAssetPath);
                return;
            }

            ApplyToPrefab(PlayerPrefabPath, hero);
            ApplyToPrefab(DesktopPlayerPrefabPath, hero);
        }

        private static void ApplyToPrefab(string prefabPath, HeroDefinition hero)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogWarning("[PlayerHeroSwitcher] Could not open " + prefabPath);
                return;
            }

            try
            {
                HeroProfile profile = root.GetComponentInChildren<HeroProfile>(true);
                if (profile == null)
                {
                    Debug.LogWarning("[PlayerHeroSwitcher] No HeroProfile on " + prefabPath);
                    return;
                }

                // definition is a private [SerializeField], so go through SerializedObject rather
                // than adding a setter to gameplay-owned code.
                SerializedObject serialized = new SerializedObject(profile);
                SerializedProperty property = serialized.FindProperty("definition");
                property.objectReferenceValue = hero;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[PlayerHeroSwitcher] " + System.IO.Path.GetFileNameWithoutExtension(prefabPath) +
                          " is now " + hero.DisplayName + " (" + hero.MaxHealth + " HP).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
