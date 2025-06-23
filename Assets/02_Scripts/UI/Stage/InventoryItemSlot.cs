using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class InventoryItemSlot : View
    {
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI itemCountText;
        [SerializeField] Button button;

        public event Action<InventoryItemSlot> OnItemSlotClickedEvent;

        void Awake()
        {
            button.onClick.AddListener(OnButtonClick);
        }

        void OnButtonClick() => OnItemSlotClickedEvent?.Invoke(this);

        public void SetItemIcon(Sprite icon)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        public void SetItemCount(int count)
        {
            itemCountText.text = count > 0 ? $"{count}" : string.Empty;
            itemCountText.enabled = count > 0;
        }
    }
}