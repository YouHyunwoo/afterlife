using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            costText.text = $"{cost}";

            var isActive = gameObject.activeSelf;
            if (!isActive) { gameObject.SetActive(true); }
            LayoutRebuilder.ForceRebuildLayoutImmediate(costText.rectTransform);
            gameObject.SetActive(isActive);

            if (state == UpgradeNode.UpgradeState.Locked)
            {
                nameText.color = Color.gray;
                descriptionText.color = Color.gray;
                costText.color = Color.gray;
            }
            else
            {
                nameText.color = Color.white;
                descriptionText.color = Color.white;
                costText.color = Color.white;
            }

            Show();
        }
    }
}