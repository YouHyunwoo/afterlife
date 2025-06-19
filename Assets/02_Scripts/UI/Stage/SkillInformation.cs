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

        public void Show(string name, string description, int cost)
        {
            nameText.text = name;
            costText.text = $"{cost}";
            descriptionText.text = description;

            var isActive = gameObject.activeSelf;
            if (!isActive) { gameObject.SetActive(true); }
            LayoutRebuilder.ForceRebuildLayoutImmediate(costText.rectTransform);
            gameObject.SetActive(isActive);

            Show();
        }
    }
}