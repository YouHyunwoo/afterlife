using Afterlife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class ItemInformation : View
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI costText;
        [SerializeField] TextMeshProUGUI descriptionText;
        [SerializeField] Image separatorImage;
        [SerializeField] TextMeshProUGUI requirementsText;
        [SerializeField] Transform requirementsContainerTransform;
        [SerializeField] GameObject requirementItemSlotPrefab;

        public void Show(string id, int cost, Data.CraftRequirement[] craftRequirements)
        {
            nameText.text = Localization.Get($"item.{id}.name");
            costText.text = $"{cost}";
            descriptionText.text = Localization.Get($"item.{id}.description");

            foreach (Transform child in requirementsContainerTransform)
            {
                Destroy(child.gameObject);
            }

            if (craftRequirements.Length > 0)
            {
                separatorImage.gameObject.SetActive(true);
                requirementsText.gameObject.SetActive(true);
                requirementsContainerTransform.gameObject.SetActive(true);

                foreach (var craftRequirement in craftRequirements)
                {
                    var craftRequirementItemSlotObject = Instantiate(requirementItemSlotPrefab, requirementsContainerTransform);
                    var craftRequirementItemSlot = craftRequirementItemSlotObject.GetComponent<ItemSlot>();

                    var itemData = ServiceLocator.Get<DataManager>().ItemDataDictionary[craftRequirement.ItemId];
                    craftRequirementItemSlot.ItemId = craftRequirement.ItemId;
                    craftRequirementItemSlot.SetItemIcon(itemData.Icon);
                    craftRequirementItemSlot.SetItemCount(craftRequirement.ItemAmount);
                    craftRequirementItemSlot.SetTargetable(false);
                }

                var rectTransform = GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 290);
            }
            else
            {
                separatorImage.gameObject.SetActive(false);
                requirementsText.gameObject.SetActive(false);
                requirementsContainerTransform.gameObject.SetActive(false);

                var rectTransform = GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 200);
            }

            var isActive = gameObject.activeSelf;
            if (!isActive) { gameObject.SetActive(true); }
            LayoutRebuilder.ForceRebuildLayoutImmediate(costText.rectTransform);
            gameObject.SetActive(isActive);

            Show();
        }
    }
}