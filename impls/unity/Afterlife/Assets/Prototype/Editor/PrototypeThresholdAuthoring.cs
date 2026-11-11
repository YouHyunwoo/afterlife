using System;
using Afterlife.Modules.Threshold;
using Afterlife.Modules.Threshold.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterlife.Prototype.Editor
{
    /// <summary>Idempotent authoring entry point for the current open prototype scene. No menu item is registered.</summary>
    public static class PrototypeThresholdAuthoring
    {
        private const string ModulePrefabs = "Assets/Modules/Threshold/Prefabs";
        private const string SamplePrefabs = "Assets/Samples/ThresholdLab/Prefabs";
        private const string PrototypeScenePath = "Assets/Prototype/Scenes/Afterlife_PixelPrototype.unity";
        private const string TitleSettingsPath = "Assets/Prototype/Prefabs/Modal/ThresholdTitleSettings.prefab";
        private const int VillagePreviewLayer = 30;

        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeScenePath) throw new InvalidOperationException("Open the saved prototype scene before applying ThresholdLab.");
            if (!AssetDatabase.IsValidFolder(ModulePrefabs)) AssetDatabase.CreateFolder("Assets/Modules/Threshold", "Prefabs");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Prototype/Resources/Fonts/Mona12TextKR SDF.asset");
            var veil = AssetDatabase.LoadAssetAtPath<Material>("Assets/Samples/ThresholdLab/Materials/WorldVeil.mat");
            var profile = AssetDatabase.LoadAssetAtPath<ThresholdProfile>("Assets/Samples/ThresholdLab/Settings/ThresholdProfile.asset");
            var audioBank = AssetDatabase.LoadAssetAtPath<ThresholdAudioBank>("Assets/Samples/ThresholdLab/Settings/ThresholdAudioBank.asset");
            if (font == null || veil == null || profile == null || audioBank == null) throw new InvalidOperationException("Threshold font, veil, profile, or audio bank is missing.");

            int layer = EnsureBackdropLayer();
            BuildPage("TitlePage", ThresholdScreen.Title, font);
            BuildPage("LobbyPage", ThresholdScreen.Lobby, font);
            BuildPage("GameOverPage", ThresholdScreen.GameOver, font);
            BuildPage("ClearPage", ThresholdScreen.Clear, font);
            BuildLoadingPage(font);
            BuildBackdrop(layer, veil, profile, audioBank);
            BuildPack();

            var canvas = GameObject.Find("Canvas");
            var gameRoot = GameObject.Find("GameRoot");
            if (canvas == null || gameRoot == null) throw new InvalidOperationException("Prototype Canvas or GameRoot is missing.");
            var modalRoot = canvas.transform.Find("ModalLayer");
            if (modalRoot == null) throw new InvalidOperationException("ModalLayer is missing.");
            var modalEffect = canvas.GetComponent<ThresholdModalEffect>();
            if (modalEffect == null) modalEffect = canvas.AddComponent<ThresholdModalEffect>();
            modalEffect.Configure(profile, audioBank);
            var settings = modalRoot.Find("Settings");
            BuildTitleSettings(settings, font);
            foreach (var name in new[] { "Loot", "Settings", "Settlement" })
            {
                var modal = modalRoot.Find(name);
                if (modal == null) throw new InvalidOperationException(name + " modal is missing.");
                var frame = modal.GetComponent<ThresholdModalFrame>();
                if (frame == null) frame = modal.gameObject.AddComponent<ThresholdModalFrame>();
                frame.Configure(font);
                frame.Apply();
            }
            // Window modals share the edge treatment. Full-screen surfaces keep their own
            // presentation, with Settings as the one explicit exception.
            foreach (Transform modal in modalRoot)
            {
                var modalSurface = modal.GetComponent<PrototypeModalSurface>();
                if (modalSurface == null) continue;
                if (modalSurface.Presentation != ModalPresentation.Window && modal != settings) continue;
                modalSurface.EdgeBackground = true;
                if (modal == settings) modalSurface.BlurBackground = false;
                EditorUtility.SetDirty(modalSurface);
            }
            var dim = modalRoot.Find("DimBackground");
            if (dim == null) throw new InvalidOperationException("DimBackground is missing.");
            var edge = dim.GetComponent<ThresholdScreenEdge>();
            if (edge == null) edge = dim.gameObject.AddComponent<ThresholdScreenEdge>();
            edge.Configure(veil);

            var oldFlow = GameObject.Find("Game Flow Screens");
            var existing = GameObject.Find("ThresholdFlowPack");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            var pack = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModulePrefabs + "/ThresholdFlowPack.prefab"), scene);
            pack.name = "ThresholdFlowPack";
            var deck = pack.GetComponentInChildren<ThresholdScreenDeck>(true);
            var rig = pack.GetComponentInChildren<ThresholdBackdropRig>(true);
            var display = pack.transform.Find("ScreenCanvas/Background")?.GetComponent<UnityEngine.UI.RawImage>();
            var loading = pack.GetComponentInChildren<ThresholdLoadingView>(true);
            var titleSettings = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TitleSettingsPath), deck.transform);
            titleSettings.name = "TitleSettings";
            titleSettings.SetActive(false);
            var adapter = deck.gameObject.AddComponent<PrototypeFlowScreen>();
            adapter.Configure(deck, rig, display, loading, titleSettings.GetComponent<PrototypeTitleSettings>());
            var view = gameRoot.GetComponent<PrototypeView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_flowScreen").objectReferenceValue = adapter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (oldFlow != null) oldFlow.SetActive(false);

            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(edge);
            EditorUtility.SetDirty(modalEffect);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "ThresholdLab prefabs and prototype scene connected.";
        }

        private static void BuildPage(string sourceName, ThresholdScreen screen, TMP_FontAsset font)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SamplePrefabs + "/" + sourceName + ".prefab");
            if (source == null) throw new InvalidOperationException(sourceName + " source prefab is missing.");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                root.name = "Threshold" + sourceName;
                foreach (var action in root.GetComponentsInChildren<ThresholdActionButton>(true)) action.enabled = false;
                var view = root.GetComponent<ThresholdScreenView>();
                if (view == null) view = root.AddComponent<ThresholdScreenView>();
                TMP_Text summary = null;
                if (screen == ThresholdScreen.GameOver || screen == ThresholdScreen.Clear)
                {
                    var label = new GameObject("ResultSummary", typeof(RectTransform), typeof(TextMeshProUGUI));
                    var rect = (RectTransform)label.transform;
                    rect.SetParent(root.transform, false);
                    rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    rect.anchoredPosition = new Vector2(screen == ThresholdScreen.GameOver ? 242 : 150, screen == ThresholdScreen.GameOver ? -615 : -675);
                    rect.sizeDelta = new Vector2(1050, 45);
                    summary = label.GetComponent<TextMeshProUGUI>();
                    summary.font = font;
                    summary.fontSize = 21;
                    summary.color = new Color(.67f, .74f, .77f);
                    summary.raycastTarget = false;
                }
                view.Configure(screen, summary);
                if (screen == ThresholdScreen.Lobby)
                {
                    var legacy = root.GetComponent<ThresholdIntro>();
                    if (legacy == null) throw new InvalidOperationException("LobbyPage is missing ThresholdIntro.");
                    legacy.enabled = false;
                    var intro = root.AddComponent<ThresholdIntroSequenceView>();
                    intro.Configure(legacy);
                }
                if (screen == ThresholdScreen.GameOver || screen == ThresholdScreen.Clear)
                {
                    var unused = root.transform.Find("Lobby");
                    if (unused != null) unused.gameObject.SetActive(false);
                }
                PrefabUtility.SaveAsPrefabAsset(root, ModulePrefabs + "/Threshold" + sourceName + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void BuildBackdrop(int layer, Material veil, ThresholdProfile profile, ThresholdAudioBank audioBank)
        {
            var root = new GameObject("ThresholdBackdropRig");
            try
            {
                var environment = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SamplePrefabs + "/VoidEnvironment.prefab"), root.transform);
                environment.name = "VoidEnvironment";
                // The prototype supplies its own gameplay world; omit the lab's trial floor.
                var trialStage = environment.transform.Find("EmptyTrialStage");
                if (trialStage != null) UnityEngine.Object.DestroyImmediate(trialStage.gameObject);
                // The portal's village preview uses layer 30 and its own camera.
                // Preserve that pair so the village appears only inside the crack.
                SetLayer(environment.transform, layer);
                environment.SetActive(false);
                var cameraObject = new GameObject("BackdropCamera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.cullingMask = 1 << layer;
                camera.enabled = false;
                var poses = new[]
                {
                    Pose(root.transform, ThresholdScreen.Title, new Vector3(-1.1f, 2.6f, -10), new Vector3(.2f, 0, -2), 50),
                    Pose(root.transform, ThresholdScreen.Lobby, new Vector3(.2f, 1.65f, -3.5f), new Vector3(.3f, 2.05f, 3), 48),
                    Pose(root.transform, ThresholdScreen.GameOver, new Vector3(30, 8, -12), new Vector3(30, 0, 0), 48),
                    Pose(root.transform, ThresholdScreen.Clear, new Vector3(.2f, 1.65f, -3.5f), new Vector3(.3f, 2.05f, 3), 48)
                };
                camera.transform.SetPositionAndRotation(poses[0].transform.position, poses[0].transform.rotation);
                camera.fieldOfView = poses[0].FieldOfView;
                root.AddComponent<ThresholdModalEffect>().Configure(profile, audioBank);
                root.AddComponent<ThresholdBackdropRig>().Configure(camera, environment, poses, veil, profile);
                PrefabUtility.SaveAsPrefabAsset(root, ModulePrefabs + "/ThresholdBackdropRig.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void BuildLoadingPage(TMP_FontAsset font)
        {
            var root = new GameObject("ThresholdLoadingPage", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(ThresholdLoadingView));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var image = root.GetComponent<UnityEngine.UI.Image>();
                image.color = new Color(.008f, .012f, .018f, 1);
                image.raycastTarget = true;

                var label = new GameObject("LoadingMessage", typeof(RectTransform), typeof(TextMeshProUGUI));
                var labelRect = (RectTransform)label.transform;
                labelRect.SetParent(root.transform, false);
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(.5f, .5f);
                labelRect.sizeDelta = new Vector2(1100, 90);
                var text = label.GetComponent<TextMeshProUGUI>();
                text.font = font;
                text.fontSize = 32;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(.87f, .91f, .93f);
                text.text = "마을을 준비하고 있습니다";
                text.raycastTarget = false;
                root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, ModulePrefabs + "/ThresholdLoadingPage.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static ThresholdCameraPose Pose(Transform parent, ThresholdScreen screen, Vector3 position, Vector3 lookAt, float fieldOfView)
        {
            var go = new GameObject(screen + "Pose");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.LookAt(parent.TransformPoint(lookAt));
            var pose = go.AddComponent<ThresholdCameraPose>();
            pose.Screen = screen;
            pose.FieldOfView = fieldOfView;
            return pose;
        }

        private static void BuildPack()
        {
            var root = new GameObject("ThresholdFlowPack");
            try
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModulePrefabs + "/ThresholdBackdropRig.prefab"), root.transform);
                var canvasObject = new GameObject("ScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform, false);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20;
                var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
                var backdrop = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
                var backgroundRect = (RectTransform)backdrop.transform;
                backgroundRect.SetParent(canvasObject.transform, false);
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;
                backdrop.GetComponent<UnityEngine.UI.RawImage>().raycastTarget = false;
                var screens = new[] { "TitlePage", "LobbyPage", "GameOverPage", "ClearPage" };
                var views = new ThresholdScreenView[screens.Length];
                ThresholdIntroSequenceView intro = null;
                for (int i = 0; i < screens.Length; i++)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePrefabs + "/Threshold" + screens[i] + ".prefab");
                    var page = (GameObject)PrefabUtility.InstantiatePrefab(asset, canvasObject.transform);
                    page.name = screens[i];
                    views[i] = page.GetComponent<ThresholdScreenView>();
                    if (screens[i] == "LobbyPage") intro = page.GetComponent<ThresholdIntroSequenceView>();
                    page.SetActive(i == 0);
                }
                var loading = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModulePrefabs + "/ThresholdLoadingPage.prefab"), canvasObject.transform);
                loading.name = "LoadingPage";
                loading.SetActive(false);
                canvasObject.AddComponent<ThresholdScreenDeck>().Configure(views, intro);
                PrefabUtility.SaveAsPrefabAsset(root, ModulePrefabs + "/ThresholdFlowPack.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void BuildTitleSettings(Transform source, TMP_FontAsset font)
        {
            if (source == null) throw new InvalidOperationException("Settings modal is missing.");
            PrefabUtility.SaveAsPrefabAsset(source.gameObject, TitleSettingsPath);
            var root = PrefabUtility.LoadPrefabContents(TitleSettingsPath);
            try
            {
                if (root.GetComponent<PrototypeTitleSettings>() == null) root.AddComponent<PrototypeTitleSettings>();
                var frame = root.GetComponent<ThresholdModalFrame>();
                if (frame == null) frame = root.AddComponent<ThresholdModalFrame>();
                frame.Configure(font);
                root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, TitleSettingsPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static int EnsureBackdropLayer()
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            for (int i = 8; i < 32; i++) if (layers.GetArrayElementAtIndex(i).stringValue == "ThresholdBackdrop") return i;
            for (int i = 8; i < 32; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                slot.stringValue = "ThresholdBackdrop";
                tags.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            throw new InvalidOperationException("No free Unity layer for ThresholdBackdrop.");
        }

        private static void SetLayer(Transform root, int layer)
        {
            if (root.gameObject.layer != VillagePreviewLayer) root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayer(child, layer);
        }
    }
}
