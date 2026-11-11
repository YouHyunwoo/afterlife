using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Afterlife.ProjectMaintenance
{
    /// <summary>에디터 에셋 API로 기존 GUID를 보존하는 일회성 폴더 이전.</summary>
    public static class AssetLayoutMigration
    {
        private static readonly string[,] Moves =
        {
            { "Assets/01_Scenes", "Assets/Legacy/Scenes" },
            { "Assets/02_Scripts", "Assets/Legacy/Scripts" },
            { "Assets/03_Prefabs", "Assets/Legacy/Prefabs" },
            { "Assets/04_Data", "Assets/Legacy/Data" },
            { "Assets/05_Resources", "Assets/Legacy/Content" },
            { "Assets/99_Dev", "Assets/Legacy/Experiments" },
            { "Assets/Editor/Tools", "Assets/Legacy/Editor/Tools" },
            { "Assets/Editor/UpgradeTree", "Assets/Legacy/Editor/UpgradeTree" },
            { "Assets/_AfterlifePrototype", "Assets/Prototype" },
            { "Assets/Afterlife/Modules", "Assets/Modules" },
            { "Assets/Afterlife/Samples", "Assets/Samples" }
        };

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play mode before moving assets.");
            // 모든 이동을 먼저 검사해 경로 충돌로 인한 부분 이전을 방지한다.
            for (var i = 0; i < Moves.GetLength(0); i++)
                if (AssetDatabase.IsValidFolder(Moves[i, 0]) && AssetDatabase.IsValidFolder(Moves[i, 1]))
                    throw new IOException("Both source and destination exist: " + Moves[i, 1]);
            EnsureFolder("Assets/Legacy/Editor");
            for (var i = 0; i < Moves.GetLength(0); i++)
            {
                var source = Moves[i, 0];
                var destination = Moves[i, 1];
                if (!AssetDatabase.IsValidFolder(source))
                {
                    if (!AssetDatabase.IsValidFolder(destination))
                        throw new DirectoryNotFoundException(source);
                    continue;
                }
                var guid = AssetDatabase.AssetPathToGUID(source);
                var error = AssetDatabase.MoveAsset(source, destination);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                if (AssetDatabase.AssetPathToGUID(destination) != guid)
                    throw new InvalidOperationException("Folder GUID changed: " + destination);
                Debug.Log("Moved " + source + " -> " + destination);
            }
            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                var path = AssetDatabase.GUIDToAssetPath(scene.guid.ToString());
                if (!string.IsNullOrEmpty(path)) scene.path = path;
            }
            EditorBuildSettings.scenes = scenes;
            if (AssetDatabase.IsValidFolder("Assets/Afterlife") &&
                Directory.GetFileSystemEntries("Assets/Afterlife").Length == 0)
                AssetDatabase.DeleteAsset("Assets/Afterlife");
            RemoveUnusedEmptyFolders();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Afterlife asset layout migration completed. No Play or tests executed.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        private static void RemoveUnusedEmptyFolders()
        {
            foreach (var path in new[]
            {
                "Assets/Legacy/Scripts/Presentation", "Assets/Legacy/Scripts/Shared",
                "Assets/Legacy/Prefabs/World", "Assets/Legacy/Experiments/Prefabs",
                "Assets/Legacy/Experiments/Scripts/View", "Assets/Legacy/Experiments/Shader",
                "Assets/Devlog/DC/07/Rendering", "Assets/Devlog/Youtube/Day01/Rendering"
            })
                if (AssetDatabase.IsValidFolder(path) && Directory.GetFileSystemEntries(path).Length == 0)
                    AssetDatabase.DeleteAsset(path);
        }
    }
}
