using Afterlife.Dev.UpgradeTree.Editor;
using Afterlife.Domain.Progression.Upgrade.Data;
using NUnit.Framework;
using UnityEngine;

namespace Afterlife.Dev.UpgradeTree.Editor.Tests
{
    public class UpgradeTreeMigrationUtilityTests
    {
        [Test]
        public void BuildNodes_GroupsNumberedLegacyUpgradeIds_AsSingleMultiLevelNode()
        {
            var nodes = UpgradeTreeMigrationUtility.BuildNodes(new[]
            {
                CreateLegacyUpgrade("attack-power-2", 10),
                CreateLegacyUpgrade("attack-power-1", 5),
                CreateLegacyUpgrade("automation", 3)
            });

            var attackPower = nodes.Find(node => node.Id == "attack-power");
            var automation = nodes.Find(node => node.Id == "automation");

            Assert.That(attackPower, Is.Not.Null);
            Assert.That(attackPower.Levels, Has.Length.EqualTo(2));
            Assert.That(attackPower.Levels[0].Cost, Is.EqualTo(5));
            Assert.That(attackPower.Levels[1].Cost, Is.EqualTo(10));
            Assert.That(attackPower.Levels[0].Description, Is.EqualTo("upgrade.attack-power-1.description"));
            Assert.That(attackPower.Name, Is.EqualTo("Attack Power"));
            Assert.That(automation, Is.Not.Null);
            Assert.That(automation.Levels, Has.Length.EqualTo(1));
        }

        private static UpgradeData CreateLegacyUpgrade(string id, int cost)
        {
            var upgradeData = ScriptableObject.CreateInstance<UpgradeData>();
            upgradeData.Id = id;
            upgradeData.Cost = cost;
            return upgradeData;
        }
    }
}
