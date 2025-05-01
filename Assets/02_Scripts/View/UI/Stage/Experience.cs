using TMPro;
using UnityEngine;

namespace Afterlife.View
{
    public class Experience : UIView
    {
        [SerializeField] TextMeshProUGUI amountText;

        public void SetExperience(float amount) => amountText.text = $"{amount:0}";
    }
}