using System;
using UnityEngine;

namespace Afterlife.Dev.UpgradeTree.Data
{
    [Serializable]
    public class UpgradeTreeNodeData
    {
        public string Id;
        public string Name;
        public Sprite Icon;
        public Vector2Int GridPosition;
        public string[] ChildNodeIds = Array.Empty<string>();
        public UpgradeTreeLevelData[] Levels = Array.Empty<UpgradeTreeLevelData>();
    }
}
