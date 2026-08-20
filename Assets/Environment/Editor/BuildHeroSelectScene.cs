using HeroVR.Experimental;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Builds a playtest scene where every hero can be switched between in VR.
    ///
    /// Starts from the graybox arena and adds a hero select controller wired to each hero's player
    /// prefab. Arena_Graybox_01 is never modified; this writes a separate scene, the same way the
    /// gameplay side keeps Arena_ThorVRTest separate.
    ///
    /// No match bootstrap is added. The select controller spawns the player itself, and a
    /// bootstrap spawning a second one would leave two XR rigs and two active cameras in the
    /// scene. A training enemy is spawned separately so there is something to fight.
    /// </summary>
    public static class BuildHeroSelectScene
    {
        private const string ArenaScenePath = "Assets/Scenes/Arenas/Arena_Graybox_01.unity";
        private const string OutputScenePath = "Assets/Scenes/Arenas/Arena_HeroSelect.unity";

        private const string SpiderPlayer = "Assets/Prefabs/Characters/XRPlayer.prefab";
        private const string ThorPlayer = "Assets/Prefabs/Characters/ThorXRPlayer.prefab";
        private const string EnemyPrefab = "Assets/Prefabs/Characters/TrainingEnemy.prefab";

        [MenuItem("Tools/HeroVR/Environment/Build Hero Select Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

            GameObject spider = AssetDatabase.LoadAssetAtPath<GameObject>(SpiderPlayer);
            GameObject thor = AssetDatabase.LoadAssetAtPath<GameObject>(ThorPlayer);

            if (spider == null || thor == null)
            {
                Debug.LogError("[BuildHeroSelectScene] Missing a player prefab. spider=" +
                               (spider != null) + " thor=" + (thor != null));
                return;
            }

            GameObject rig = new GameObject("HeroSelect");
            HeroSelectController controller = rig.AddComponent<HeroSelectController>();

            // Private serialized fields, so populate through SerializedObject rather than adding
            // setters to the component for the editor's benefit.
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty list = serialized.FindProperty("heroes");
            list.arraySize = 2;

            SetHero(list.GetArrayElementAtIndex(0), "Spider-Man", spider);
            SetHero(list.GetArrayElementAtIndex(1), "Thor", thor);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Spawn the starting player where the arena's own Team A spawn sits, so the hero
            // starts on the designated spawn rather than at world origin.
            Transform spawn = GameObject.Find("TeamA_Spawn_1")?.transform;
            Vector3 position = spawn != null ? spawn.position : new Vector3(0f, .6f, 5f);
            Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;

            GameObject startingPlayer = (GameObject)PrefabUtility.InstantiatePrefab(spider, scene);
            startingPlayer.name = spider.name;
            startingPlayer.transform.SetPositionAndRotation(position, rotation);

            SpawnEnemy(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OutputScenePath);

            Debug.Log("[BuildHeroSelectScene] Wrote " + OutputScenePath +
                      ". Press left Y (or Tab on desktop) to cycle heroes. " +
                      "Arena_Graybox_01 untouched.");
        }

        private static void SetHero(SerializedProperty element, string displayName, GameObject prefab)
        {
            element.FindPropertyRelative("displayName").stringValue = displayName;
            element.FindPropertyRelative("playerPrefab").objectReferenceValue = prefab;
        }

        private static void SpawnEnemy(Scene scene)
        {
            GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
            if (enemy == null)
                return;

            Transform spawn = GameObject.Find("TeamB_Spawn_1")?.transform;
            Vector3 position = spawn != null ? spawn.position : new Vector3(0f, 1f, -8f);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(enemy, scene);
            instance.name = enemy.name;

            // TrainingEnemy's capsule is centred on its origin, so it sits at waist height.
            instance.transform.position = position + Vector3.up;
        }
    }
}
