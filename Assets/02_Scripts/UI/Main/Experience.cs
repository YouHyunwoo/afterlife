using TMPro;
using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Experience : UI.View
    {
        [SerializeField] TextMeshProUGUI amountText;

        public void SetExperience(float amount) => amountText.text = $"{amount:0}";
    }
}