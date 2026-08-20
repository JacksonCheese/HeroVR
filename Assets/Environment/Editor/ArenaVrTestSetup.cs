using HeroVR.Arena;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Builds a throwaway PCVR playtest scene: the graybox arena with the gameplay branch's XR
    /// player and training enemy wired into the arena's spawn transforms.
    ///
    /// This exists so the environment can be judged at real VR scale, which is the one thing the
    /// editor view and the automated traversal tests cannot tell us. It writes a separate scene
    /// and never modifies Arena_Graybox_01.
    /// </summary>
    public static class ArenaVrTestSetup
    {
        private const string ArenaScenePath = "Assets/Scenes/Arenas/Arena_Graybox_01.unity";
        private const string VrScenePath = "Assets/Scenes/Arenas/Arena_VRTest.unity";
        private const string XrBootstrapPath = "Assets/Prefabs/Gameplay/XRGameplayMatchBootstrap.prefab";

        [MenuItem("Tools/HeroVR/Environment/Build VR Playtest Scene")]
        public static void BuildVrTestScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

            ConfigureSpawn("TeamA_Spawn_1", ArenaTeam.TeamOne, 1, ArenaSpawnType.Player);
            ConfigureSpawn("TeamA_Spawn_2", ArenaTeam.TeamOne, 2, ArenaSpawnType.Player);
            ConfigureSpawn("TeamB_Spawn_1", ArenaTeam.TeamTwo, 1, ArenaSpawnType.TrainingEnemy);
            ConfigureSpawn("TeamB_Spawn_2", ArenaTeam.TeamTwo, 2, ArenaSpawnType.TrainingEnemy);

            GameObject bootstrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrBootstrapPath);
            if (bootstrapPrefab == null)
            {
                Debug.LogError("[ArenaVrTestSetup] Missing " + XrBootstrapPath);
                return;
            }

            GameObject bootstrap = (GameObject)PrefabUtility.InstantiatePrefab(bootstrapPrefab, scene);
            bootstrap.name = "XRGameplayMatchBootstrap";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, VrScenePath);

            Debug.Log("[ArenaVrTestSetup] Wrote " + VrScenePath +
                      ". Arena_Graybox_01 is untouched. Standalone XR must be initialising for the " +
                      "headset to engage.");
        }

        private static void ConfigureSpawn(string spawnName, ArenaTeam team, int slot, ArenaSpawnType type)
        {
            GameObject spawn = GameObject.Find(spawnName);
            if (spawn == null)
            {
                Debug.LogError("[ArenaVrTestSetup] Spawn transform not found: " + spawnName);
                return;
            }

            ArenaSpawnPoint point = spawn.GetComponent<ArenaSpawnPoint>();
            if (point == null)
                point = spawn.AddComponent<ArenaSpawnPoint>();

            point.Configure(team, slot, type);
            EditorUtility.SetDirty(point);
        }
    }
}
