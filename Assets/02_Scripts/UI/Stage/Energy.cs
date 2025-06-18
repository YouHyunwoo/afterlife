using TMPro;
using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class Energy : UI.View
    {
        [SerializeField] TextMeshProUGUI energyAmountText;

        public void SetEnergy(float amount) => energyAmountText.text = $"{amount:F0}";
    }
}