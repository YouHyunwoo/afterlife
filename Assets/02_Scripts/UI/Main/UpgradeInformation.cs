using TMPro;
using UnityEngine;

namespace Afterlife.UI.Main
{
    public class UpgradeInformation : View
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI descriptionText;
        [SerializeField] TextMeshProUGUI costText;
        [SerializeField] GameObject lockedOverlay;

        public void Show(string name, string description, int cost, UpgradeNode.UpgradeState state)
        {
            nameText.text = name;
            descriptionText.text = description;
            // costText.text = cost.ToString();

            if (state == UpgradeNode.UpgradeState.Locked)
            {
                nameText.color = Color.gray;
                descriptionText.color = Color.gray;
            }
            else
            {
                nameText.color = Color.white;
                descriptionText.color = Color.white;
            }

            Show();
        }
    }
}