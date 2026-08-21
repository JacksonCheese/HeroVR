using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Adds a NavMeshSurface to the arena scenes and bakes navigation.
    ///
    /// Environment-side half of bot navigation. The gameplay side already drives TrainingBot from
    /// a NavMeshAgent with a direct-steering fallback, but nothing had ever been baked, so the
    /// agent had no surface, the fallback ran permanently, and the bot walked into towers.
    ///
    /// Every arena scene is baked rather than just Arena_Graybox_01. The test scenes were copied
    /// from the arena before navigation existed, so they are independent scene files and do not
    /// inherit a surface added to the original.
    ///
    /// Agent settings are matched to the training enemy's capsule (radius 0.5, height 2) rather
    /// than left at Unity's defaults, so the baked mesh reflects where that bot can actually fit.
    /// </summary>
    public static class ArenaNavMeshBaker
    {
        private static readonly string[] ArenaScenes =
        {
            "Assets/Scenes/Arenas/Arena_Graybox_01.unity",
            "Assets/Scenes/Arenas/Arena_ThorVRTest.unity",
            "Assets/Scenes/Arenas/Arena_ThorDesktopTest.unity",
            "Assets/Scenes/Arenas/Arena_VRTest.unity",
            "Assets/Scenes/Arenas/Arena_HeroSelect.unity",
            "Assets/Scenes/Arenas/Arena_DestructionTest.unity",
            "Assets/Scenes/Arenas/BossArena_Graybox_01.unity"
        };

        private const string SurfaceName = "NavMeshSurface";

        // TrainingEnemy's CapsuleCollider: radius 0.5, height 2. Baking with Unity's defaults
        // would claim gaps the bot cannot physically fit through.
        private const float AgentRadius = .5f;
        private const float AgentHeight = 2f;

        // The bot is Rigidbody-driven with no step handling, so it can only manage what physics
        // rolls it over. Kept just under the 0.25m plaza risers.
        private const float AgentClimb = .24f;
        private const float AgentSlope = 45f;

        [MenuItem("Tools/HeroVR/Environment/Bake Arena NavMesh")]
        public static void BakeAll()
        {
            List<string> baked = new List<string>();
            List<string> skipped = new List<string>();

            foreach (string scenePath in ArenaScenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    skipped.Add(scenePath + " (missing)");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                NavMeshSurface surface = FindOrCreateSurface(scene);

                ConfigureAgent(surface);
                surface.BuildNavMesh();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                bool hasData = surface.navMeshData != null;
                baked.Add(System.IO.Path.GetFileNameWithoutExtension(scenePath) +
                          (hasData ? " ok" : " NO DATA"));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ArenaNavMeshBaker] Baked: " + string.Join(", ", baked) +
                      (skipped.Count > 0 ? " | Skipped: " + string.Join(", ", skipped) : ""));
        }

        /// <summary>
        /// Places the surface on the Environment root and bakes only its children. Collecting the
        /// whole scene instead would bake the player and training enemy into the floor as solid
        /// geometry, since both carry colliders.
        /// </summary>
        private static NavMeshSurface FindOrCreateSurface(Scene scene)
        {
            GameObject environment = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Environment")
                    environment = root;

                NavMeshSurface existing = root.GetComponentInChildren<NavMeshSurface>(true);
                if (existing != null)
                    return existing;
            }

            if (environment == null)
            {
                Debug.LogWarning("[ArenaNavMeshBaker] No Environment root in " + scene.name +
                                 "; falling back to a scene-wide surface.");
                return new GameObject(SurfaceName).AddComponent<NavMeshSurface>();
            }

            return environment.AddComponent<NavMeshSurface>();
        }

        private static void ConfigureAgent(NavMeshSurface surface)
        {
            SerializedObject serialized = new SerializedObject(surface);

            // Override the agent profile rather than editing the shared Humanoid type, which
            // would change navigation for anything else that uses it.
            SetInt(serialized, "m_OverrideTileSize", 0);
            SetInt(serialized, "m_OverrideVoxelSize", 0);

            // Children of the Environment root only, so runtime objects - the player, the enemy,
            // a thrown Mjolnir - are never baked into the floor as static geometry.
            SetInt(serialized, "m_CollectObjects", (int)CollectObjects.Children);
            SetInt(serialized, "m_UseGeometry", (int)NavMeshCollectGeometry.PhysicsColliders);
            SetInt(serialized, "m_DefaultArea", 0);
            SetBool(serialized, "m_IgnoreNavMeshAgent", true);
            SetBool(serialized, "m_IgnoreNavMeshObstacle", true);

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Agent size lives on the NavMesh build settings, addressed by agent type index.
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(surface.agentTypeID);
            settings.agentRadius = AgentRadius;
            settings.agentHeight = AgentHeight;
            settings.agentClimb = AgentClimb;
            settings.agentSlope = AgentSlope;
        }

        private static void SetInt(SerializedObject serialized, string field, int value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property != null)
                property.intValue = value;
        }

        private static void SetBool(SerializedObject serialized, string field, bool value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property != null)
                property.boolValue = value;
        }
    }
}
