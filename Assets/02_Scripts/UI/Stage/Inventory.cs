using System;

namespace Afterlife.UI.Stage
{
    public class Inventory : View
    {
        public InventoryItemSlot[] ItemSlots;

        public event Action<InventoryItemSlot> OnItemSlotClickedEvent;

        void Awake()
        {
            ItemSlots = GetComponentsInChildren<InventoryItemSlot>();

            foreach (var itemSlot in ItemSlots)
            {
                itemSlot.OnItemSlotClickedEvent += OnItemSlotClicked;
                itemSlot.SetItemIcon(null);
                itemSlot.SetEquippedIcon(false);
                itemSlot.SetItemCount(0);
            }
        }

        void OnItemSlotClicked(InventoryItemSlot slot) => OnItemSlotClickedEvent?.Invoke(slot);
    }
}