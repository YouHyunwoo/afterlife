using Afterlife.Dev.UpgradeTree.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Afterlife.Dev.UpgradeTree.Editor
{
    public static class UpgradeTreeSceneInstaller
    {
        private const string CanvasPrefabPath = "Assets/Legacy/Prefabs/UI/Main/Dev.Upgrade/Upgrade Tree Canvas.prefab";
        private const string MainScenePath = "Assets/Legacy/Scenes/Main.unity";
        private const string TestScenePath = "Assets/Legacy/Scenes/Upgrade Tree Test.unity";
        private const string CanvasName = "Upgrade Tree Canvas";

        [MenuItem("Tools/Afterlife/Upgrade Tree/Install V2 Canvas In Scenes")]
        public static void InstallV2CanvasInScenes()
        {
            if (Application.isBatchMode)
            {
                Debug.LogError("Install V2 Canvas In Scenes is intended to run from the open Unity Editor menu, not batchmode.");
                return;
            }

            InstallInMainScene();
            InstallInTestScene();
            AssetDatabase.SaveAssets();
        }

        public static void InstallInMainScene()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath);
            var upgradeTreeView = EnsureCanvasInstance();
            upgradeTreeView.gameObject.SetActive(false);

            var mainScreen = Object.FindFirstObjectByType<Afterlife.UI.Main.Screen>(FindObjectsInactive.Include);
            if (mainScreen == null)
            {
                Debug.LogError("Main Screen not found.");
                return;
            }

            var serializedScreen = new SerializedObject(mainScreen);
            serializedScreen.FindProperty("UpgradeTreeView").objectReferenceValue = upgradeTreeView;
            serializedScreen.ApplyModifiedPropertiesWithoutUndo();

            DisableLegacyUpgradePanel();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void InstallInTestScene()
        {
            var scene = EditorSceneManager.OpenScene(TestScenePath);
            var existingCanvas = GameObject.Find(CanvasName);
            if (existingCanvas != null)
            {
                Object.DestroyImmediate(existingCanvas);
            }

            var upgradeTreeView = InstantiateCanvas();
            upgradeTreeView.gameObject.SetActive(true);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static UpgradeTreeView EnsureCanvasInstance()
        {
            var existingCanvas = GameObject.Find(CanvasName);
            if (existingCanvas != null)
            {
                var existingView = existingCanvas.GetComponentInChildren<UpgradeTreeView>(true);
                if (existingView != null)
                {
                    return existingView;
                }

                Object.DestroyImmediate(existingCanvas);
            }

            return InstantiateCanvas();
        }

        private static UpgradeTreeView InstantiateCanvas()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath);
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"Upgrade tree canvas prefab not found: {CanvasPrefabPath}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = CanvasName;

            var upgradeTreeView = instance.GetComponentInChildren<UpgradeTreeView>(true);
            if (upgradeTreeView == null)
            {
                throw new System.InvalidOperationException("UpgradeTreeView not found in upgrade tree canvas prefab.");
            }

            return upgradeTreeView;
        }

        private static void DisableLegacyUpgradePanel()
        {
            var legacyPanels = Object.FindObjectsByType<Afterlife.Domain.Progression.Upgrade.UI.UpgradePanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var panel in legacyPanels)
            {
                if (panel != null)
                {
                    panel.gameObject.SetActive(false);
                }
            }
        }
    }
}
