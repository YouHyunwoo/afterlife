using Afterlife.Core;
using UnityEngine;

namespace Afterlife.Model
{
    public class Upgrade
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
