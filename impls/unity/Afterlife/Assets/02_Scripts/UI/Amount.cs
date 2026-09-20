using TMPro;
using UnityEngine;

namespace Afterlife.UI
{
    public class Amount : View
    {
        [SerializeField] TextMeshProUGUI amountText;

        public void SetAmount(float amount) => amountText.text = $"<sprite=0> {amount:0}";
    }
}