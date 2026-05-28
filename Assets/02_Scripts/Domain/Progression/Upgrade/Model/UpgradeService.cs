using Afterlife.Core;
using Afterlife.Model;
using UnityEngine;

namespace Afterlife.Domain.Progression.Upgrade.Model
{
    public class UpgradeService
    {
        public void ApplyUpgrade(string upgradeId)
        {
            var upgradeData = ServiceLocator.Get<DataManager>().UpgradeDataDictionary[upgradeId];
            if (upgradeData == null)
            {
                Debug.LogError($"Upgrade data not found for ID: {upgradeId}");
                return;
            }

            foreach (var parameter in upgradeData.Parameters)
            {
                PlayerParameterApplicator.Apply(parameter);
            }
        }
    }
}
