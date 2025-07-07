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

        public void Show(string id, int cost, Data.CraftRequirement[] requirements)
        {
            nameText.text = Localization.Get($"item.{id}.name");
            costText.text = $"{cost}";
            descriptionText.text = Localization.Get($"item.{id}.description");

            foreach (Transform child in requirementsContainerTransform)
            {
                Destroy(child.gameObject);
            }

            if (requirements.Length > 0)
            {
                separatorImage.gameObject.SetActive(true);
                requirementsText.gameObject.SetActive(true);
                requirementsContainerTransform.gameObject.SetActive(true);

                foreach (var requirement in requirements)
                {
                    var requirementItemSlotObject = Instantiate(requirementItemSlotPrefab, requirementsContainerTransform);
                    var requirementItemSlot = requirementItemSlotObject.GetComponent<ItemSlot>();

                    var itemData = ServiceLocator.Get<DataManager>().ItemDataDictionary[requirement.ItemId];
                    requirementItemSlot.ItemId = requirement.ItemId;
                    requirementItemSlot.SetItemIcon(itemData.Icon);
                    requirementItemSlot.SetItemCount(requirement.Amount);
                    requirementItemSlot.SetTargetable(false);
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