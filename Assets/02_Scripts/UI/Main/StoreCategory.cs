using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class StoreCategory : UI.View
    {
        public VerticalLayoutGroup VerticalLayoutGroup;
        public StoreItem StoreItemPrefab;
        public TextMeshProUGUI NameText;

        public event Action<string> OnStoreItemClickedEvent;

        public void SetName(string name) => NameText.text = name;

        public void SetStoreItems(Data.StoreItem[] storeItemDataArray)
        {
            foreach (Transform child in VerticalLayoutGroup.transform)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < storeItemDataArray.Length; i++)
            {
                var storeItemData = storeItemDataArray[i];
                var storeItem = Instantiate(StoreItemPrefab, VerticalLayoutGroup.transform);
                storeItem.SetName(storeItemData.StoreStepItems[0].Name);
                storeItem.SetDescription(storeItemData.StoreStepItems[0].Description);
                storeItem.SetCost(storeItemData.StoreStepItems[0].Cost.ToString());
                storeItem.OnButtonClickedEvent += () => OnStoreItemClickedEvent?.Invoke(storeItemData.Id);
            }

            if (VerticalLayoutGroup.TryGetComponent<RectTransform>(out var rectTransform))
            {
                var interval = VerticalLayoutGroup.spacing;
                var itemHeight = StoreItemPrefab.GetComponent<RectTransform>().sizeDelta.y;
                var contentHeight = storeItemDataArray.Length * (itemHeight + interval) - interval;
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, contentHeight);
            }
            else
            {
                Debug.LogError("RectTransform component not found on StoreCategory prefab.");
            }
        }
    }
}