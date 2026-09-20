using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class CraftItemSlot : View
    {
        [SerializeField] Image iconImage;
        [SerializeField] Image lockedOverlayImage;
        [SerializeField] Button button;

        public string ItemId;

        public event Action<CraftItemSlot> OnItemSlotClickedEvent;

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

        public void SetLocked(bool isLocked)
        {
            lockedOverlayImage.enabled = isLocked;
        }
    }
}