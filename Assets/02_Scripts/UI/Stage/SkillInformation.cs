using Afterlife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class SkillInformation : View
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI costText;
        [SerializeField] Image separatorImage;
        [SerializeField] TextMeshProUGUI descriptionText;

        public void Show(string id, int cost)
        {
            nameText.text = Localization.Get($"upgrade.{id}.name");
            costText.text = $"{cost}";
            descriptionText.text = Localization.Get($"upgrade.{id}.description");

            var isActive = gameObject.activeSelf;
            if (!isActive) { gameObject.SetActive(true); }
            LayoutRebuilder.ForceRebuildLayoutImmediate(costText.rectTransform);
            gameObject.SetActive(isActive);

            Show();
        }
    }
}