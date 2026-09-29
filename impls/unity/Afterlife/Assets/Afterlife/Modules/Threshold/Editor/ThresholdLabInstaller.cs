using System;
using System.IO;
using Afterlife.Modules.Threshold.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Afterlife.Modules.Threshold.Editor
{
    /// <summary>Explicit authoring request only. Never opens Play Mode, replaces an existing asset, or saves the user's scene.</summary>
    [InitializeOnLoad]
    internal static class ThresholdLabInstaller
    {
        internal const string Root = "Assets/Afterlife/Samples/ThresholdLab";
        internal const string ScenePath = Root + "/Scenes/Afterlife_ThresholdLab.unity";
        private const string Request = "Temp/threshold-lab.request";
        private const string Report = "Temp/threshold-lab.result.txt";
        static ThresholdLabInstaller() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var command = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            if (command == "connect-audio")
            {
                try { ThresholdAudioAuthoring.Apply(); File.WriteAllText(Report, "SAVED: licensed music, motion/water cues, seamless water bed, audio prefab and in-game credits. No Play Mode or tests run."); }
                catch (Exception error) { File.WriteAllText(Report, "FAILED: " + error); Debug.LogException(error); }
                return;
            }
            if (command == "connect-day01-after")
            {
                try { ConnectDay01After(); File.WriteAllText(Report, "SAVED: Day01 final-stage runtime presentation connected to village camera. No Play Mode or tests run."); }
                catch (Exception error) { File.WriteAllText(Report, "FAILED: " + error); Debug.LogException(error); }
                return;
            }
            if (command == "deform-surfaces")
            {
                try { ThresholdSurfaceAuthoring.Apply(); File.WriteAllText(Report, "SAVED: dense surface meshes and URP reflection lights. No Play Mode or tests run."); }
                catch (Exception error) { File.WriteAllText(Report, "FAILED: " + error); Debug.LogException(error); }
                return;
            }
            if (command == "create-intro")
            {
                try { ThresholdLabIntroAuthoring.Apply(); File.WriteAllText(Report, "SAVED: display camera, visible puddle, skippable subtitle intro and portal entry. No Play Mode or tests run."); }
                catch (Exception error) { File.WriteAllText(Report, "FAILED: " + error); Debug.LogException(error); }
                return;
            }
            if (command == "refine-surfaces")
            {
                try { ThresholdLabRefinement.Apply(); File.WriteAllText(Report, "SAVED: bounded puddle, Day01 After portal, black void, reversible .95s travel and result backdrop. No Play Mode or tests run."); }
                catch (Exception error) { File.WriteAllText(Report, "FAILED: " + error); Debug.LogException(error); }
                return;
            }
            if (command == "finish-authoring")
            {
                try { FinishAuthoring(); File.WriteAllText(Report, "SAVED: missing group bindings and non-playing capture visibility repaired. No tests or Play Mode run."); }
                catch (Exception error) { File.WriteAllText(Report, "FAILED: " + error); Debug.LogException(error); }
                return;
            }
            if (command != "create") { File.WriteAllText(Report, "IGNORED: expected create."); return; }
            try { Install(); File.WriteAllText(Report, "CREATED: " + ScenePath + "\nPrefab assets saved. Existing scenes preserved. Play Mode and tests not run."); }
            catch (Exception error) { File.WriteAllText(Report, "FAILED: " + error); Debug.LogException(error); }
        }

        internal static void Install()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("The lab already exists. Edit its prefabs; existing scenes are never overwritten.");
            foreach (var folder in new[] { "Scenes", "Prefabs", "Materials", "Settings" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var profile = Asset<ThresholdProfile>(Root + "/Settings/ThresholdProfile.asset", () => ScriptableObject.CreateInstance<ThresholdProfile>());
                var root = new GameObject("ThresholdLab");
                var host = root.AddComponent<ThresholdHost>(); host.Profile = profile;
                UnityEditor.Events.UnityEventTools.AddPersistentListener(host.QuitRequested, host.ExitApplication);
                var cameraObject = new GameObject("WorldCamera", typeof(Camera), typeof(AudioListener)); cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.001f, .002f, .004f);
                camera.nearClipPlane = .08f; camera.farClipPlane = 160; camera.cullingMask = ~(1 << 30); camera.fieldOfView = 48;
                host.ViewCamera = camera;
                var poses = new GameObject("CameraPoses"); poses.transform.SetParent(root.transform, false);
                Pose(poses.transform, ThresholdScreen.Title, new Vector3(-1.1f, 1.7f, -12), new Vector3(.2f, .4f, 2), 50);
                Pose(poses.transform, ThresholdScreen.Lobby, new Vector3(.2f, 1.65f, -3.5f), new Vector3(.3f, 2.05f, 3), 48);
                Pose(poses.transform, ThresholdScreen.Playing, new Vector3(30, 8, -12), new Vector3(30, 0, 0), 48);
                Pose(poses.transform, ThresholdScreen.GameOver, new Vector3(-.8f, 1.5f, -8), new Vector3(.2f, 1, 3), 50);
                Pose(poses.transform, ThresholdScreen.Clear, new Vector3(.2f, 1.65f, -3.5f), new Vector3(.3f, 2.05f, 3), 48);
                camera.transform.SetPositionAndRotation(poses.transform.GetChild(0).position, poses.transform.GetChild(0).rotation);

                var environment = ThresholdLabWorld.Build();
                PrefabChild(environment, "VoidEnvironment", root.transform);

                var canvasObject = new GameObject("ScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform, false);
                var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
                var backdrop = ThresholdLabUi.Rect("WorldCapture", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var raw = backdrop.gameObject.AddComponent<RawImage>(); raw.color = Color.white; raw.raycastTarget = false; raw.enabled = false;
                var veil = backdrop.gameObject.AddComponent<ThresholdVeil>(); veil.SourceCamera = camera;
                veil.Template = Material("WorldVeil", "Afterlife/Threshold/Veil", Color.white);

                foreach (ThresholdScreen screen in Enum.GetValues(typeof(ThresholdScreen)))
                    PrefabChild(ThresholdLabUi.Page(screen), screen + "Page", canvasObject.transform);
                PrefabChild(ThresholdLabUi.Modal(), "ModalPage", canvasObject.transform);
                // Stored scene preview: title visible before entering Play Mode, all other pages remain authored but transparent.
                foreach (var page in root.GetComponentsInChildren<ThresholdPage>(true))
                {
                    if (page.Group == null) page.Group = page.GetComponent<CanvasGroup>();
                    page.Group.alpha = !page.IsModal && page.Screen == ThresholdScreen.Title ? 1 : 0;
                    page.Group.blocksRaycasts = page.Group.alpha > .5f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(page);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(page.Group);
                }
                var audio = new GameObject("SubmergedAmbience", typeof(AudioSource), typeof(AudioLowPassFilter), typeof(ThresholdAmbience)); audio.transform.SetParent(root.transform, false);
                audio.GetComponent<AudioSource>().playOnAwake = false;
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.transform.SetParent(root.transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                var prefabPath = Root + "/Prefabs/ThresholdExperience.prefab";
                if (File.Exists(prefabPath)) throw new InvalidOperationException("Experience prefab already exists; refusing to overwrite.");
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                ThresholdLabRefinement.Apply();
                ThresholdLabIntroAuthoring.Apply();
                ThresholdSurfaceAuthoring.Apply();
                ConnectDay01After();
                ThresholdAudioAuthoring.Apply();
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        private static void ConnectDay01After()
        {
            // Optional sample authoring: the reusable Threshold runtime has no Day01 assembly dependency.
            var type = Type.GetType("Afterlife.Devlog.Day01.Day01FinalStagePreview, Afterlife.Devlog.Day01");
            if (type == null) throw new InvalidOperationException("Day01 preview adapter has not compiled yet.");
            string path = Root + "/Prefabs/VoidEnvironment.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var preview = root.transform.Find("Day01AfterPreview");
                var stage = preview.GetChild(0);
                var component = preview.GetComponent(type);
                if (component == null) component = preview.gameObject.AddComponent(type);
                var serialized = new SerializedObject(component);
                serialized.FindProperty("Stage").objectReferenceValue = stage;
                serialized.FindProperty("ViewCamera").objectReferenceValue = root.GetComponentInChildren<ThresholdPortal>(true).SourceCamera;
                serialized.FindProperty("Profile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Afterlife/Modules/SceneFeatures/Presets/After.asset");
                string smokePath = AssetDatabase.GUIDToAssetPath("b7ba647810f70ba41987cfd1b4ce6f5b");
                var smoke = AssetDatabase.LoadAssetAtPath<GameObject>(smokePath);
                if (smoke != null) serialized.FindProperty("SmokePrefab").objectReferenceValue = smoke.GetComponent<ParticleSystem>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
        private static void Pose(Transform parent, ThresholdScreen screen, Vector3 position, Vector3 lookAt, float fov)
        {
            var obj = new GameObject(screen + "CameraPose"); obj.transform.SetParent(parent, false); obj.transform.position = position; obj.transform.LookAt(lookAt);
            var pose = obj.AddComponent<ThresholdCameraPose>(); pose.Screen = screen; pose.FieldOfView = fov;
        }
        private static GameObject PrefabChild(GameObject source, string name, Transform parent)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            // Resume an interrupted first installation without replacing any previously authored prefab.
            if (!File.Exists(path)) PrefabUtility.SaveAsPrefabAsset(source, path);
            UnityEngine.Object.DestroyImmediate(source);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
            return instance;
        }
        internal static T Asset<T>(string path, Func<T> create) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = create(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static void FinishAuthoring()
        {
            foreach (var path in new[] { Root + "/Prefabs/ModalPage.prefab", Root + "/Prefabs/ThresholdExperience.prefab" })
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var page in contents.GetComponentsInChildren<ThresholdPage>(true))
                        if (page.Group == null) page.Group = page.GetComponent<CanvasGroup>();
                    foreach (var veil in contents.GetComponentsInChildren<ThresholdVeil>(true)) veil.GetComponent<RawImage>().enabled = false;
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            AssetDatabase.SaveAssets();
        }
        internal static Material Material(string name, string shaderName, Color color)
        {
            return Asset(Root + "/Materials/" + name + ".mat", () =>
            {
                var shader = Shader.Find(shaderName); if (shader == null) throw new InvalidOperationException("Missing shader: " + shaderName);
                var material = new Material(shader) { name = name };
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.color = color;
                return material;
            });
        }
    }

    [CustomEditor(typeof(ThresholdProfile))]
    internal sealed class ThresholdProfileInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("ThresholdLab is a separate authoring sample. This button never starts Play Mode or overwrites an existing scene.", MessageType.Info);
            if (GUILayout.Button("Create Separate Threshold Lab"))
            {
                try { ThresholdLabInstaller.Install(); }
                catch (Exception error) { Debug.LogException(error); }
            }
            if (GUILayout.Button("Select Threshold Lab Scene")) Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ThresholdLabInstaller.ScenePath);
        }
    }
}
