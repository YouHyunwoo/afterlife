using System;
using Afterlife.Modules.Threshold.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Modules.Threshold.Editor
{
    internal static class ThresholdLabIntroAuthoring
    {
        internal static void Apply()
        {
            Edit("LobbyPage", ThresholdLabUi.ConfigureIntro);
            Edit("ThresholdExperience", root =>
            {
                var host = root.GetComponent<ThresholdHost>();
                var portal = root.GetComponentInChildren<ThresholdPortal>(true);
                host.PortalEntry = portal.Surface.transform;
                var output = root.transform.Find("DisplayCamera");
                if (output == null)
                {
                    output = new GameObject("DisplayCamera", typeof(Camera)).transform;
                    output.SetParent(root.transform, false);
                }
                var camera = output.GetComponent<Camera>();
                camera.enabled = true; camera.targetTexture = null; camera.cullingMask = 0;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                // Draw after offscreen cameras; the overlay Canvas presents their captured image.
                camera.depth = 10; camera.targetDisplay = 0;
                host.ViewCamera.enabled = true;
                var title = root.transform.Find("CameraPoses/TitleCameraPose");
                title.localPosition = new Vector3(-1.1f, 2.6f, -10);
                title.LookAt(root.transform.TransformPoint(new Vector3(.2f, 0, -2)));
                PrefabUtility.RecordPrefabInstancePropertyModifications(host);
            });
            foreach (string name in new[] { "PlayingPage", "GameOverPage", "ClearPage" })
                Edit(name, root => {
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                        text.text = text.text.Replace("로비 다시 보기", "인트로 다시 보기").Replace("로비로", "인트로 다시 보기");
                });
            var pool = AssetDatabase.LoadAssetAtPath<Material>(ThresholdLabInstaller.Root + "/Materials/Pool.mat");
            pool.SetColor("_Tint", new Color(.18f, .27f, .31f)); EditorUtility.SetDirty(pool);
            var profile = AssetDatabase.LoadAssetAtPath<ThresholdProfile>(ThresholdLabInstaller.Root + "/Settings/ThresholdProfile.asset");
            profile.WorldTransitionSeconds = 1.8f; EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
        private static void Edit(string name, Action<GameObject> change)
        {
            string path = ThresholdLabInstaller.Root + "/Prefabs/" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try { change(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
