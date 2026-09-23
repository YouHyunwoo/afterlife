using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    // 기존 검 FBX만 참조해 제단 프리팹에 소품을 추가한다. 원본 메시를 변경하지 않는다.
    [InitializeOnLoad]
    internal static class Day01AltarOfferingSetup
    {
        private const string PrefabPath = Day01SceneBuilder.Root + "/Prefabs/Environment/AbandonedAltarV2.prefab";
        private const string SwordPath = "Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/SwordTool.fbx";
        static Day01AltarOfferingSetup() { EditorApplication.delayCall += Apply; }

        private static void Apply()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += Apply; return; }
            if (!File.Exists(PrefabPath)) return;
            var sword = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Day01SceneBuilder.Root + "/Materials/AfterModels.mat");
            if (sword == null || material == null) return;
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.transform.Find("OfferingSword") != null) return;
                var holder = new GameObject("OfferingSword").transform;
                holder.SetParent(root.transform, false);
                holder.localRotation = Quaternion.Euler(0, 65, 0) * Quaternion.Euler(-90, 0, 0);
                holder.localScale = Vector3.one * 1.2f;
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(sword, holder);
                visual.transform.localPosition = sword.transform.localPosition;
                visual.transform.localRotation = sword.transform.localRotation;
                visual.transform.localScale = sword.transform.localScale;
                var bounds = new Bounds();
                bool hasBounds = false;
                foreach (var filter in visual.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = filter.GetComponent<Renderer>();
                    if (renderer == null || filter.sharedMesh == null) continue;
                    var slots = new Material[filter.sharedMesh.subMeshCount];
                    for (int i = 0; i < slots.Length; i++) slots[i] = material;
                    renderer.sharedMaterials = slots;
                    Day01ContentUpgrade.EnsureReadable(filter.sharedMesh);
                    foreach (var vertex in filter.sharedMesh.vertices)
                    {
                        var point = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                        else bounds.Encapsulate(point);
                    }
                    var surface = filter.gameObject.AddComponent<ShowcasePickSurface>();
                    surface.VisibleRenderer = renderer;
                }
                if (!hasBounds) throw new InvalidOperationException("Existing sword has no visible mesh.");
                // 실제 정점의 최저점을 상판(1.035m)에 맞추고 XZ 중심을 정렬한다.
                holder.localPosition = new Vector3(-bounds.center.x, 1.039f - bounds.min.y, -.065f - bounds.center.z);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.."));
                File.WriteAllText(Path.Combine(projectRoot, "devlog/youtube/records/altar-offering-unity.json"),
                    "{\"source\":\"" + SwordPath + "\",\"prefab\":\"" + PrefabPath +
                    "\",\"savedAtUtc\":\"" + DateTime.UtcNow.ToString("O") + "\",\"newModelCreated\":false}");
                Debug.Log("Devlog: existing SwordTool placed on altar tabletop; prefab saved.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
