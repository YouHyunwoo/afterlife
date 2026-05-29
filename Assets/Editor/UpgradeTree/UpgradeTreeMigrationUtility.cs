using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Afterlife.Dev.UpgradeTree.Data;
using Afterlife.Domain.Progression.Upgrade.Data;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Dev.UpgradeTree.Editor
{
    public static class UpgradeTreeMigrationUtility
    {
        private static readonly Regex LevelIdRegex = new(@"^(?<base>.+)-(?<level>\d+)$", RegexOptions.Compiled);

        [MenuItem("Tools/Afterlife/Upgrade Tree/Migrate Legacy Upgrade Assets")]
        public static void MigrateLegacyUpgradeAssets()
        {
            var outputPath = EditorUtility.SaveFilePanelInProject(
                "Create Upgrade Tree Data",
                "DevUpgradeTreeData",
                "asset",
                "Select where to create the migrated upgrade tree asset.");
            if (string.IsNullOrEmpty(outputPath)) { return; }

            var legacyAssets = AssetDatabase.FindAssets("t:UpgradeData", new[] { "Assets/04_Data/Upgrade" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<UpgradeData>)
                .Where(asset => asset != null)
                .ToArray();

            var treeData = BuildTreeData(legacyAssets);
            AssetDatabase.CreateAsset(treeData, outputPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = treeData;
        }

        public static UpgradeTreeData BuildTreeData(IReadOnlyCollection<UpgradeData> legacyAssets)
        {
            var treeData = ScriptableObject.CreateInstance<UpgradeTreeData>();
            treeData.Nodes = BuildNodes(legacyAssets).ToArray();
            return treeData;
        }

        public static List<UpgradeTreeNodeData> BuildNodes(IReadOnlyCollection<UpgradeData> legacyAssets)
        {
            var groupedAssets = new Dictionary<string, List<(int LevelNumber, UpgradeData Data)>>();
            if (legacyAssets != null)
            {
                foreach (var legacyAsset in legacyAssets)
                {
                    if (legacyAsset == null || string.IsNullOrEmpty(legacyAsset.Id)) { continue; }

                    var match = LevelIdRegex.Match(legacyAsset.Id);
                    var baseId = match.Success ? match.Groups["base"].Value : legacyAsset.Id;
                    var levelNumber = match.Success ? int.Parse(match.Groups["level"].Value) : 1;

                    if (!groupedAssets.TryGetValue(baseId, out var group))
                    {
                        group = new List<(int LevelNumber, UpgradeData Data)>();
                        groupedAssets.Add(baseId, group);
                    }

                    group.Add((levelNumber, legacyAsset));
                }
            }

            var nodes = new List<UpgradeTreeNodeData>();
            var column = 0;
            foreach (var pair in groupedAssets.OrderBy(pair => pair.Key))
            {
                var levels = pair.Value
                    .OrderBy(entry => entry.LevelNumber)
                    .ThenBy(entry => entry.Data.Id, StringComparer.Ordinal)
                    .Select(entry => new UpgradeTreeLevelData
                    {
                        Cost = entry.Data.Cost,
                        Description = $"upgrade.{entry.Data.Id}.description",
                        Parameters = entry.Data.Parameters ?? Array.Empty<Afterlife.Domain.Progression.Effect.Data.PlayerEffectParameter>()
                    })
                    .ToArray();

                nodes.Add(new UpgradeTreeNodeData
                {
                    Id = pair.Key,
                    Name = CreateName(pair.Key),
                    Icon = pair.Value.OrderBy(entry => entry.LevelNumber).First().Data.Icon,
                    GridPosition = new Vector2Int(column % 6, column / 6),
                    ChildNodeIds = Array.Empty<string>(),
                    Levels = levels
                });
                column++;
            }

            return nodes;
        }

        private static string CreateName(string id)
        {
            if (string.IsNullOrEmpty(id)) { return string.Empty; }

            return string.Join(" ", id
                .Split('-')
                .Where(part => !string.IsNullOrEmpty(part))
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        }
    }
}
