using System;
using Afterlife.Dev.UpgradeTree.Data;
using Afterlife.Domain.Progression.Effect.Data;

namespace Afterlife.Dev.UpgradeTree.Model
{
    public class UpgradeTreeService
    {
        public UpgradeTreeNodeState GetNodeState(
            UpgradeTreeData treeData,
            UpgradeTreeNodeData node,
            UpgradeTreeProgress progress)
        {
            if (!IsVisible(treeData, node, progress)) { return UpgradeTreeNodeState.Hidden; }
            return IsCompleted(node, progress) ? UpgradeTreeNodeState.Completed : UpgradeTreeNodeState.Available;
        }

        public bool IsVisible(UpgradeTreeData treeData, UpgradeTreeNodeData node, UpgradeTreeProgress progress)
        {
            if (node == null) { return false; }
            if (treeData == null || treeData.Nodes == null) { return true; }

            progress ??= new UpgradeTreeProgress();
            foreach (var parentNode in treeData.Nodes)
            {
                if (parentNode == null || parentNode.ChildNodeIds == null) { continue; }
                if (!Array.Exists(parentNode.ChildNodeIds, childNodeId => childNodeId == node.Id)) { continue; }

                if (progress.GetAcquiredLevelCount(parentNode.Id) <= 0) { return false; }
            }

            return true;
        }

        public bool IsCompleted(UpgradeTreeNodeData node, UpgradeTreeProgress progress)
        {
            if (node == null || node.Levels == null || node.Levels.Length == 0) { return true; }
            progress ??= new UpgradeTreeProgress();
            return progress.GetAcquiredLevelCount(node.Id) >= node.Levels.Length;
        }

        public UpgradeTreeLevelData GetNextLevel(UpgradeTreeNodeData node, UpgradeTreeProgress progress)
        {
            if (node == null || node.Levels == null) { return null; }

            progress ??= new UpgradeTreeProgress();
            var levelIndex = progress.GetAcquiredLevelCount(node.Id);
            if (levelIndex < 0 || levelIndex >= node.Levels.Length) { return null; }
            return node.Levels[levelIndex];
        }

        public UpgradeTreePurchaseResult TryPurchase(
            UpgradeTreeData treeData,
            string nodeId,
            UpgradeTreeProgress progress,
            Func<int, bool> spendCost,
            Action<PlayerEffectParameter> applyParameter)
        {
            var node = treeData != null ? treeData.FindNode(nodeId) : null;
            if (node == null) { return UpgradeTreePurchaseResult.NodeNotFound; }

            progress ??= new UpgradeTreeProgress();
            if (!IsVisible(treeData, node, progress)) { return UpgradeTreePurchaseResult.Hidden; }
            if (IsCompleted(node, progress)) { return UpgradeTreePurchaseResult.Completed; }

            var nextLevel = GetNextLevel(node, progress);
            if (nextLevel == null) { return UpgradeTreePurchaseResult.MissingLevel; }
            if (spendCost != null && !spendCost(nextLevel.Cost)) { return UpgradeTreePurchaseResult.CannotAfford; }

            if (nextLevel.Parameters != null && applyParameter != null)
            {
                foreach (var parameter in nextLevel.Parameters)
                {
                    if (parameter == null) { continue; }
                    applyParameter(parameter);
                }
            }

            progress.IncrementAcquiredLevelCount(node.Id);
            return UpgradeTreePurchaseResult.Purchased;
        }
    }
}
