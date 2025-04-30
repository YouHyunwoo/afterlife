using TMPro;
using UnityEngine;

namespace Afterlife.View
{
    public class Energy : UIView
    {
        [SerializeField] TextMeshProUGUI energyAmountText;

        public void SetEnergy(float amount) => energyAmountText.text = $"{amount:F0}";
    }
}