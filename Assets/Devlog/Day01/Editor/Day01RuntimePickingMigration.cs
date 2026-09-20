using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    // 명시적 요청을 받아 저장 데이터 참조를 제거한다. 열린 씬/미저장 작업은 저장하지 않는다.
    [InitializeOnLoad]
    internal static class Day01RuntimePickingMigration
    {
        private const string Request = "Library/Day01RuntimePickingMigration.request";
        private const string Folder = "Assets/Devlog/Day01/Picking";

        [Serializable] private sealed class Report
        {
            public string utc;
            public string[] readableModels, reserializedPrefabs;
            public int deletedPickingAssets;
            public bool completed;
        }

        static Day01RuntimePickingMigration() { EditorApplication.delayCall += Apply; }

        private static void Apply()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += Apply; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try
            {
                var paths = AssetDatabase.FindAssets("t:ShowcasePickMesh", new[] { Folder })
                    .Select(AssetDatabase.GUIDToAssetPath).ToArray();
                var guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
                var readable = new HashSet<string>();
                foreach (string path in paths)
                {
                    string key = Path.GetFileNameWithoutExtension(path);
                    int split = key.LastIndexOf('_');
                    string source = key == "AbandonedAltarV2" ? Day01AltarModelSetup.ModelPath
                        : split > 0 ? AssetDatabase.GUIDToAssetPath(key.Substring(0, split)) : null;
                    if (string.IsNullOrEmpty(source)) continue;
                    if (AssetImporter.GetAtPath(source) is ModelImporter importer && !importer.isReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                        readable.Add(source);
                    }
                }
                var references = FindReferences(guids, paths);
                if (references.Any(p => !p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Non-prefab Picking references require migration: " + string.Join(", ", references));
                // ForceReserializeAssets는 알 수 없는 구형 필드를 보존할 수 있어 실제 컴포넌트로 저장한다.
                foreach (string path in references)
                {
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                            throw new IOException("Prefab migration failed: " + path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                var remaining = FindReferences(guids, paths);
                if (remaining.Length != 0)
                    throw new InvalidOperationException("Picking references remain: " + string.Join(", ", remaining));

                // 현재/향후 사용하는 모든 Surface의 원본도 준비한다. 원본을 복사하지 않는다.
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Day01SceneBuilder.Root }))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    foreach (var surface in prefab.GetComponentsInChildren<ShowcasePickSurface>(true))
                    {
                        var filter = surface.GetComponent<MeshFilter>();
                        if (filter == null || filter.sharedMesh == null)
                            throw new InvalidOperationException("Picking surface has no source MeshFilter: " + surface.name);
                        Day01ContentUpgrade.EnsureReadable(filter.sharedMesh);
                    }
                }
                if (AssetDatabase.IsValidFolder(Folder) && !AssetDatabase.DeleteAsset(Folder))
                    throw new IOException("Cannot remove Picking folder.");
                // 과거 저장 데이터 보정기는 런타임 생성 전환으로 더 이상 필요하지 않다.
                AssetDatabase.DeleteAsset(Day01SceneBuilder.Root + "/Editor/Day01PickColorUpgrade.cs");
                var report = new Report { utc = DateTime.UtcNow.ToString("O"), readableModels = readable.OrderBy(p => p).ToArray(),
                    reserializedPrefabs = references, deletedPickingAssets = paths.Length, completed = true };
                File.WriteAllText("Library/Day01RuntimePickingMigration.json", JsonUtility.ToJson(report, true));
                Debug.Log($"Runtime Picking migration: {paths.Length} assets removed, {references.Length} prefabs migrated, {readable.Count} models readable.");
            }
            catch (Exception error)
            {
                File.WriteAllText("Library/Day01RuntimePickingMigration.error.txt", error.ToString());
                Debug.LogException(error);
            }
        }

        private static string[] FindReferences(string[] guids, string[] excluded)
        {
            var skip = new HashSet<string>(excluded, StringComparer.OrdinalIgnoreCase);
            return Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => (p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".asset")) && !skip.Contains(p))
                .Where(p => { string text = File.ReadAllText(p); return guids.Any(g => text.Contains(g)); })
                .OrderBy(p => p).ToArray();
        }
    }
}
