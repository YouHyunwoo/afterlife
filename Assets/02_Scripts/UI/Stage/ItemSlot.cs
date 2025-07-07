using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class ItemSlot : View,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] Image iconImage;
        [SerializeField] Image equippedIconImage;
        [SerializeField] TextMeshProUGUI countText;
        [SerializeField] Image lockedOverlayImage;
        [SerializeField] Button button;

        public string ItemId;

        public event Action<ItemSlot> OnItemSlotClickedEvent;
        public event Action<ItemSlot> OnInformationShowed;
        public event Action<ItemSlot> OnInformationHidden;

        void Awake()
        {
            button.onClick.AddListener(OnButtonClick);
        }

        void OnButtonClick() => OnItemSlotClickedEvent?.Invoke(this);

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            OnInformationShowed?.Invoke(this);
        }
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => OnInformationHidden?.Invoke(this);

        public void SetItemIcon(Sprite icon)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        public void SetEquippedIcon(bool isEquipped)
        {
            equippedIconImage.enabled = isEquipped;
        }

        public void SetItemCount(int count)
        {
            countText.text = count > 0 ? $"{count}" : string.Empty;
            countText.enabled = count > 0;
        }

        public void SetLocked(bool isLocked)
        {
            lockedOverlayImage.enabled = isLocked;
        }

        public void SetTargetable(bool isTargetable)
        {
            iconImage.raycastTarget = isTargetable;
        }
    }
}