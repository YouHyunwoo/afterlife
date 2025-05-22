using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class StoreContent : UIView
    {
        public StoreCategory StoreCategoryPrefab;
        public VerticalLayoutGroup VerticalLayoutGroup;

        public event Action<string> OnStoreItemClickedEvent;

        void Awake()
        {
            VerticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
        }

        public void SetStoreContent(Data.Store storeData)
        {
            foreach (Transform child in VerticalLayoutGroup.transform)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < storeData.StoreCategories.Length; i++)
            {
                var storeCategoryData = storeData.StoreCategories[i];
                AddStoreCategory(storeCategoryData);
            }
        }

        void AddStoreCategory(Data.StoreCategory storeCategoryData)
        {
            StoreCategory storeCategory = Instantiate(StoreCategoryPrefab, VerticalLayoutGroup.transform);
            storeCategory.SetName(storeCategoryData.Name);
            storeCategory.SetStoreItems(storeCategoryData.StoreItems);
            storeCategory.OnStoreItemClickedEvent += OnStoreItemClicked;

            if (storeCategory.TryGetComponent<RectTransform>(out var rectTransform))
            {
                var interval = VerticalLayoutGroup.spacing;
                var categoryHeight = storeCategory.VerticalLayoutGroup.GetComponent<RectTransform>().sizeDelta.y;
                var contentHeight = storeCategoryData.StoreItems.Length * (categoryHeight + interval) - interval;
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, contentHeight);
            }
            else
            {
                Debug.LogError("RectTransform component not found on StoreCategory prefab.");
            }
        }

        void OnStoreItemClicked(string storeItemId) => OnStoreItemClickedEvent?.Invoke(storeItemId);
    }
}