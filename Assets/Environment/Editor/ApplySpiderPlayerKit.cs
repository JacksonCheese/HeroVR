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

                // Glove models hang off the tracked controllers, not the physics hands. The
                // physics hands are scaled 0.18 (which would shrink a real-size model) and lag
                // the true pose, so mounting the visual on the controller keeps what the player
                // sees aligned with where a web actually fires.
                int gloves = 0;
                gloves += MountGlove(leftAim, "Assets/Environment/Heroes/Env_SpiderGlove_L.prefab");
                gloves += MountGlove(rightAim, "Assets/Environment/Heroes/Env_SpiderGlove_R.prefab");

                // With a proper glove shown, the blob mesh would just intersect it. Renderers are
                // disabled rather than removed so this stays reversible, and so the physics hand
                // keeps its collider and PunchHitbox.
                int hidden = 0;
                hidden += HideRenderers(leftHand);
                hidden += HideRenderers(rightHand);

                // Fallback: if the glove prefabs are missing, keep the old red blobs visible
                // rather than leaving the player with no hands at all.
                int recoloured = 0;
                if (gloves == 0)
                {
                    recoloured += Recolour(leftHand, glove);
                    recoloured += Recolour(rightHand, glove);
                    ShowRenderers(leftHand);
                    ShowRenderers(rightHand);
                    Debug.LogWarning("[ApplySpiderPlayerKit] Glove prefabs not found; fell back " +
                                     "to recolouring the placeholder hands. Run Build Spider Gloves.");
                }

                WebSwingLocomotion swing = root.GetComponent<WebSwingLocomotion>();
                if (swing == null)
                    swing = root.AddComponent<WebSwingLocomotion>();

                // Private serialized fields, so assign through SerializedObject.
                SerializedObject serialized = new SerializedObject(swing);
                AssignReference(serialized, "leftAim", leftAim);
                AssignReference(serialized, "rightAim", rightAim);
                // Draw webs and aim rays from the controllers, which is where the glove models now
                // sit. The physics hands trail slightly behind, so drawing from them would put
                // the web visibly off the glove it is supposed to leave from.
                AssignReference(serialized, "leftHand", leftAim);
                AssignReference(serialized, "rightHand", rightAim);
                AssignReference(serialized, "head", camera);

                // Values already serialized on the prefab win over changed script defaults, so the
                // tuning has to be written explicitly or old values silently persist. This is the
                // one place to retune the swing.
                AssignFloat(serialized, "maxWebRange", 35f);
                AssignFloat(serialized, "aimAssistRadius", .25f);

                // Positive tilts the aim down. The controller's forward axis sits higher than
                // where the index finger points, so webs fired flat read as going out too high.
                AssignFloat(serialized, "aimPitchOffset", 15f);

                // Muzzle at the index finger rather than the tracked origin, which sits behind
                // and above the trigger.
                AssignVector3(serialized, "webOriginOffset", 0f, -.022f, .045f);

                // Lighter than real gravity so falls hang and arcs read as superhero. Reel-in is
                // raised to compensate, since weaker gravity puts less energy into the pendulum.
                AssignFloat(serialized, "gravity", -13f);
                AssignFloat(serialized, "reelInSpeed", 5.2f);

                AssignFloat(serialized, "minRopeLength", 2.5f);
                AssignFloat(serialized, "airControl", 5f);
                AssignFloat(serialized, "releaseBoost", 4.5f);
                AssignFloat(serialized, "attachSpeedCarry", 5f);
                AssignFloat(serialized, "maxSpeed", 30f);

                // Kick along the arc on attach, so catching a web while stationary swings instead
                // of leaving the player hanging still.
                AssignFloat(serialized, "attachImpulse", 9f);

                // Automatic pump, kept low. It only offsets the energy the rope constraint bleeds
                // off; the player's own arm should be supplying the speed.
                AssignFloat(serialized, "swingThrust", 3.5f);

                // Arm motion is the main drive. Throwing your hand along the arc accelerates you.
                AssignFloat(serialized, "handMotionThrust", 3.2f);
                AssignFloat(serialized, "handMotionDeadzone", .35f);
                AssignFloat(serialized, "maxHandMotionSpeed", 6f);

                // Let a swing run out along the ground rather than stopping dead on contact.
                // Raised and softened after landings still killed momentum too readily.
                AssignFloat(serialized, "groundedExitSpeed", 7f);
                AssignFloat(serialized, "landingFriction", 8f);
                AssignFloat(serialized, "missVisualDuration", .35f);
                AssignFloat(serialized, "minAnchorHeightAboveFeet", 1.5f);

                // Thicker web and a visible aim ray, so a shot can never be missed on screen.
                AssignFloat(serialized, "webThickness", .05f);
                AssignBool(serialized, "showAimRay", true);
                AssignFloat(serialized, "aimRayThickness", .012f);
                AssignFloat(serialized, "aimRayLength", 12f);

                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);

                Debug.Log("[ApplySpiderPlayerKit] gloves mounted=" + gloves +
                          ", placeholder renderers hidden=" + hidden +
                          ", recoloured fallback=" + recoloured +
                          ". aim=" + Name(leftAim) + "/" + Name(rightAim) +
                          " draw=" + Name(leftHand) + "/" + Name(rightHand) +
                          " head=" + Name(camera));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private const string GloveChildName = "SpiderGlove";

        /// <summary>Mounts a glove prefab on a controller, replacing any previous one.</summary>
        private static int MountGlove(Transform mount, string prefabPath)
        {
            if (mount == null)
                return 0;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return 0;

            Transform existing = mount.Find(GloveChildName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mount);
            instance.name = GloveChildName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return 1;
        }

        private static int HideRenderers(Transform target)
        {
            if (target == null)
                return 0;

            int count = 0;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
                count++;
            }

            return count;
        }

        private static void ShowRenderers(Transform target)
        {
            if (target == null)
                return;

            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
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

        private static void AssignVector3(
            SerializedObject serialized, string field, float x, float y, float z)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[ApplySpiderPlayerKit] No serialized field named " + field);
                return;
            }

            property.vector3Value = new Vector3(x, y, z);
        }

        private static void AssignBool(SerializedObject serialized, string field, bool value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[ApplySpiderPlayerKit] No serialized field named " + field);
                return;
            }

            property.boolValue = value;
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
