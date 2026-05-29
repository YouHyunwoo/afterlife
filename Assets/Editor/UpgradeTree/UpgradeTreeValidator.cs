using System.Collections.Generic;
using Afterlife.Dev.UpgradeTree.Data;

namespace Afterlife.Dev.UpgradeTree.Editor
{
    public static class UpgradeTreeValidator
    {
        public static List<string> Validate(UpgradeTreeData treeData)
        {
            var errors = new List<string>();
            if (treeData == null)
            {
                errors.Add("Tree data is null.");
                return errors;
            }

            var nodeIds = new HashSet<string>();
            var duplicatedNodeIds = new HashSet<string>();
            if (treeData.Nodes != null)
            {
                foreach (var node in treeData.Nodes)
                {
                    if (node == null) { continue; }
                    if (string.IsNullOrEmpty(node.Id))
                    {
                        errors.Add("Node id is empty.");
                        continue;
                    }

                    if (!nodeIds.Add(node.Id))
                    {
                        duplicatedNodeIds.Add(node.Id);
                    }
                }
            }

            foreach (var duplicatedNodeId in duplicatedNodeIds)
            {
                errors.Add($"Duplicated node id: {duplicatedNodeId}");
            }

            if (treeData.Nodes == null) { return errors; }

            foreach (var node in treeData.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id)) { continue; }
                ValidateConnections(errors, nodeIds, node.Id, node.ChildNodeIds, "child");
            }

            ValidateChildCycles(errors, treeData);
            return errors;
        }

        private static void ValidateConnections(
            List<string> errors,
            HashSet<string> nodeIds,
            string sourceNodeId,
            string[] targetNodeIds,
            string connectionType)
        {
            if (targetNodeIds == null) { return; }

            var localIds = new HashSet<string>();
            foreach (var targetNodeId in targetNodeIds)
            {
                if (string.IsNullOrEmpty(targetNodeId))
                {
                    errors.Add($"{sourceNodeId} has empty {connectionType} connection.");
                    continue;
                }

                if (targetNodeId == sourceNodeId)
                {
                    errors.Add($"{sourceNodeId} has self {connectionType} connection.");
                }

                if (!nodeIds.Contains(targetNodeId))
                {
                    errors.Add($"{sourceNodeId} references missing {connectionType} node: {targetNodeId}");
                }

                if (!localIds.Add(targetNodeId))
                {
                    errors.Add($"{sourceNodeId} has duplicated {connectionType} connection: {targetNodeId}");
                }
            }
        }

        private static void ValidateChildCycles(List<string> errors, UpgradeTreeData treeData)
        {
            var nodeMap = treeData.CreateNodeMap();
            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();

            foreach (var node in treeData.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id)) { continue; }
                if (HasChildCycle(node.Id, nodeMap, visiting, visited))
                {
                    errors.Add($"Child connection cycle contains node: {node.Id}");
                }
            }
        }

        private static bool HasChildCycle(
            string nodeId,
            Dictionary<string, UpgradeTreeNodeData> nodeMap,
            HashSet<string> visiting,
            HashSet<string> visited)
        {
            if (visited.Contains(nodeId)) { return false; }
            if (!visiting.Add(nodeId)) { return true; }
            if (!nodeMap.TryGetValue(nodeId, out var node))
            {
                visiting.Remove(nodeId);
                return false;
            }

            if (node.ChildNodeIds != null)
            {
                foreach (var childNodeId in node.ChildNodeIds)
                {
                    if (string.IsNullOrEmpty(childNodeId)) { continue; }
                    if (HasChildCycle(childNodeId, nodeMap, visiting, visited)) { return true; }
                }
            }

            visiting.Remove(nodeId);
            visited.Add(nodeId);
            return false;
        }
    }
}
