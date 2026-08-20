using UnityEditor;
using UnityEngine;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Swaps the training enemy's placeholder capsule mesh for the Env_ThugEnemy body.
    ///
    /// This is a visual-only change to a gameplay-owned prefab: it removes the root MeshFilter and
    /// MeshRenderer and parents the thug model underneath. No components, colliders, or values that
    /// affect behaviour are touched, and the CapsuleCollider stays exactly as it is.
    ///
    /// Removing the root renderer is deliberate rather than just disabling it. TrainingBot resolves
    /// its attack-telegraph target with GetComponentInChildren&lt;Renderer&gt;(), which still returns a
    /// disabled renderer, so leaving it in place would make the telegraph recolour an invisible
    /// mesh. With it gone, the model's ChestPlate becomes the first renderer and the telegraph
    /// lands on the enemy's chest.
    /// </summary>
    public static class ApplyThugEnemyVisual
    {
        private const string EnemyPrefabPath = "Assets/Prefabs/Characters/TrainingEnemy.prefab";
        private const string ThugModelPath = "Assets/Environment/Enemies/Env_ThugEnemy.prefab";
        private const string ModelChildName = "ThugBody";

        [MenuItem("Tools/HeroVR/Environment/Apply Thug Visual To Training Enemy")]
        public static void Apply()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ThugModelPath);
            if (model == null)
            {
                Debug.LogError("[ApplyThugEnemyVisual] Missing " + ThugModelPath +
                               ". Run Build Thug Enemy Model first.");
                return;
            }

            GameObject enemyRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            if (enemyRoot == null)
            {
                Debug.LogError("[ApplyThugEnemyVisual] Could not open " + EnemyPrefabPath);
                return;
            }

            try
            {
                // Drop any previous application so this can be re-run safely.
                Transform existing = enemyRoot.transform.Find(ModelChildName);
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);

                // Remove the capsule mesh. Renderer first, then filter.
                MeshRenderer renderer = enemyRoot.GetComponent<MeshRenderer>();
                if (renderer != null)
                    Object.DestroyImmediate(renderer, true);

                MeshFilter filter = enemyRoot.GetComponent<MeshFilter>();
                if (filter != null)
                    Object.DestroyImmediate(filter, true);

                GameObject body = (GameObject)PrefabUtility.InstantiatePrefab(model, enemyRoot.transform);
                body.name = ModelChildName;
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;

                // Confirm the telegraph will land on the chest rather than something incidental.
                Renderer first = enemyRoot.GetComponentInChildren<Renderer>();
                string firstName = first != null ? first.name : "<none>";

                PrefabUtility.SaveAsPrefabAsset(enemyRoot, EnemyPrefabPath);

                Debug.Log("[ApplyThugEnemyVisual] Applied thug body to " + EnemyPrefabPath +
                          ". CapsuleCollider untouched. Attack telegraph will recolour: " + firstName);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(enemyRoot);
            }
        }
    }
}
