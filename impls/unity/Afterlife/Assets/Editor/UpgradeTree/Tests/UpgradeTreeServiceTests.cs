using Afterlife.Dev.UpgradeTree.Data;
using Afterlife.Dev.UpgradeTree.Model;
using NUnit.Framework;
using UnityEngine;

namespace Afterlife.Dev.UpgradeTree.Editor.Tests
{
    public class UpgradeTreeServiceTests
    {
        [Test]
        public void IsVisible_ReturnsFalse_UntilAllParentsHaveAtLeastOneLevel()
        {
            var treeData = ScriptableObject.CreateInstance<UpgradeTreeData>();
            treeData.Nodes = new[]
            {
                CreateNode("root", childNodeIds: new[] { "branch" }),
                CreateNode("branch")
            };
            var progress = new UpgradeTreeProgress();
            var service = new UpgradeTreeService();

            Assert.That(service.IsVisible(treeData, treeData.FindNode("branch"), progress), Is.False);

            progress.SetAcquiredLevelCount("root", 1);

            Assert.That(service.IsVisible(treeData, treeData.FindNode("branch"), progress), Is.True);
        }

        [Test]
        public void TryPurchase_PurchasesLevelsSequentially_AndReturnsCompletedAfterMaxLevel()
        {
            var treeData = ScriptableObject.CreateInstance<UpgradeTreeData>();
            treeData.Nodes = new[]
            {
                CreateNode("attack", levels: new[]
                {
                    new UpgradeTreeLevelData { Cost = 5, Description = "Level 1" },
                    new UpgradeTreeLevelData { Cost = 10, Description = "Level 2" }
                })
            };
            var progress = new UpgradeTreeProgress();
            var service = new UpgradeTreeService();
            var spentCost = 0;

            var firstResult = service.TryPurchase(treeData, "attack", progress, cost =>
            {
                spentCost += cost;
                return true;
            }, null);
            var secondResult = service.TryPurchase(treeData, "attack", progress, cost =>
            {
                spentCost += cost;
                return true;
            }, null);
            var thirdResult = service.TryPurchase(treeData, "attack", progress, _ => true, null);

            Assert.That(firstResult, Is.EqualTo(UpgradeTreePurchaseResult.Purchased));
            Assert.That(secondResult, Is.EqualTo(UpgradeTreePurchaseResult.Purchased));
            Assert.That(thirdResult, Is.EqualTo(UpgradeTreePurchaseResult.Completed));
            Assert.That(progress.GetAcquiredLevelCount("attack"), Is.EqualTo(2));
            Assert.That(spentCost, Is.EqualTo(15));
        }

        [Test]
        public void IsVisible_RequiresEveryParentToHaveAtLeastOneLevel()
        {
            var treeData = ScriptableObject.CreateInstance<UpgradeTreeData>();
            treeData.Nodes = new[]
            {
                CreateNode("source", childNodeIds: new[] { "target" }),
                CreateNode("gate", childNodeIds: new[] { "target" }),
                CreateNode("target")
            };
            var progress = new UpgradeTreeProgress();
            progress.SetAcquiredLevelCount("source", 1);
            var service = new UpgradeTreeService();

            Assert.That(service.IsVisible(treeData, treeData.FindNode("target"), progress), Is.False);

            progress.SetAcquiredLevelCount("gate", 1);

            Assert.That(service.IsVisible(treeData, treeData.FindNode("target"), progress), Is.True);
        }

        private static UpgradeTreeNodeData CreateNode(
            string id,
            string[] childNodeIds = null,
            UpgradeTreeLevelData[] levels = null)
        {
            return new UpgradeTreeNodeData
            {
                Id = id,
                Name = id,
                ChildNodeIds = childNodeIds ?? System.Array.Empty<string>(),
                Levels = levels ?? new[] { new UpgradeTreeLevelData { Cost = 1 } }
            };
        }
    }
}
