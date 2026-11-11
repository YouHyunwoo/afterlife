using System;
using Afterlife.Modules.Threshold.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Modules.Threshold.Editor
{
    /// <summary>Scoped sample authoring; preserves prefab identities and the source Day01 assets.</summary>
    internal static class ThresholdLabRefinement
    {
        private const string AfterPath = "Assets/Devlog/Youtube/Day01/Prefabs/Stages/After_PixelForest.prefab";
        internal static void Apply()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(AfterPath);
            if (source == null) throw new InvalidOperationException("Day01 After source prefab is missing.");
            Edit("VoidEnvironment", root =>
            {
                var placeholder = root.transform.Find("VillagePreviewOnly");
                if (placeholder != null) UnityEngine.Object.DestroyImmediate(placeholder.gameObject);
                var preview = root.transform.Find("Day01AfterPreview");
                if (preview == null)
                {
                    preview = new GameObject("Day01AfterPreview").transform;
                    preview.SetParent(root.transform, false);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        behaviour.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    }
                    foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                    {
                        child.gameObject.layer = 30;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                    }
                    var lightObject = new GameObject("PreviewSun", typeof(Light));
                    lightObject.transform.SetParent(preview, false);
                    lightObject.transform.localRotation = Quaternion.Euler(45, -35, 0);
                    var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional;
                    light.intensity = 1.2f; light.color = new Color(1, .94f, .83f); light.cullingMask = 1 << 30;
                    light.shadows = LightShadows.None;
                }
                preview.localPosition = new Vector3(100, 0, 0);
                var portal = root.GetComponentInChildren<ThresholdPortal>(true);
                var camera = portal.SourceCamera;
                camera.transform.localPosition = new Vector3(110, 13, -16);
                camera.transform.LookAt(root.transform.TransformPoint(new Vector3(100, .5f, 0)));
                camera.orthographic = true; camera.orthographicSize = 8; camera.farClipPlane = 65;
                camera.backgroundColor = Color.black;
                var puddle = root.GetComponent<ThresholdPuddleSurface>() ?? root.AddComponent<ThresholdPuddleSurface>();
                puddle.Surfaces = new[] { root.transform.Find("ObsidianPuddle").GetComponent<Renderer>(), portal.Reflection };
            });
            Edit("ThresholdExperience", root => root.GetComponent<ThresholdHost>().ViewCamera.backgroundColor = Color.black);
            Edit("LobbyPage", root =>
            {
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                    if (label.text.Contains("임시 아트")) label.text = "Day01 After · 실제 세계를 바라보는 창";
            });
            var profile = AssetDatabase.LoadAssetAtPath<ThresholdProfile>(ThresholdLabInstaller.Root + "/Settings/ThresholdProfile.asset");
            profile.CameraTravelSeconds = .95f; profile.ResultTransitionSeconds = .8f;
            profile.ResultBackdrop = ThresholdResultBackdrop.KeepWorld; EditorUtility.SetDirty(profile);
            var stone = AssetDatabase.LoadAssetAtPath<Material>(ThresholdLabInstaller.Root + "/Materials/DarkStone.mat");
            if (stone != null) { stone.SetColor("_BaseColor", Color.black); EditorUtility.SetDirty(stone); }
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
