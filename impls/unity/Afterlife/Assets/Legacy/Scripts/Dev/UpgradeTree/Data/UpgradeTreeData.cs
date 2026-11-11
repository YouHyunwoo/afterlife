using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.UpgradeTree.Data
{
    [CreateAssetMenu(fileName = "UpgradeTreeData", menuName = "Afterlife/Dev/Upgrade Tree")]
    public class UpgradeTreeData : ScriptableObject
    {
        [Header("Grid")]
        public Vector2 CellSize = new(80f, 80f);
        public Vector2 NodeSize = new(50f, 50f);

        [Header("Nodes")]
        public UpgradeTreeNodeData[] Nodes = Array.Empty<UpgradeTreeNodeData>();

        public UpgradeTreeNodeData FindNode(string id)
        {
            if (string.IsNullOrEmpty(id) || Nodes == null) { return null; }

            foreach (var node in Nodes)
            {
                if (node != null && node.Id == id) { return node; }
            }

            return null;
        }

        public Dictionary<string, UpgradeTreeNodeData> CreateNodeMap()
        {
            var nodeMap = new Dictionary<string, UpgradeTreeNodeData>();
            if (Nodes == null) { return nodeMap; }

            foreach (var node in Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id)) { continue; }
                nodeMap[node.Id] = node;
            }

            return nodeMap;
        }
    }
}
