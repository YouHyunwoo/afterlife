using Afterlife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Domain.Progression.Upgrade.UI
{
    public class UpgradeTooltipView : Afterlife.UI.View
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI costText;
        [SerializeField] Image separatorImage;
        [SerializeField] TextMeshProUGUI descriptionText;

        public void Show(string id, int cost, UpgradeNodeView.UpgradeState state)
        {
            nameText.text = LocalizationManager.Get($"upgrade.{id}.name");
            costText.text = $"{cost}";
            descriptionText.text = LocalizationManager.Get($"upgrade.{id}.description");

            var isActive = gameObject.activeSelf;
            if (!isActive) { gameObject.SetActive(true); }
            LayoutRebuilder.ForceRebuildLayoutImmediate(costText.rectTransform);
            gameObject.SetActive(isActive);

            if (state == UpgradeNodeView.UpgradeState.Locked)
            {
                nameText.color = Color.gray;
                costText.color = Color.gray;
                separatorImage.color = Color.gray;
                descriptionText.color = Color.gray;
            }
            else
            {
                nameText.color = Color.white;
                costText.color = Color.white;
                separatorImage.color = Color.white;
                descriptionText.color = Color.white;
            }

            Show();
        }
    }
}
