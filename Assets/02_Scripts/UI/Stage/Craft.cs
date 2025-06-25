using System;
using Afterlife.Core;
using Afterlife.GameSystem.Stage;

namespace Afterlife.UI.Stage
{
    public class Craft : View
    {
        public CraftItemSlot[] ItemSlots;

        public event Action<CraftItemSlot> OnItemSlotClickedEvent;

        void Awake()
        {
            ItemSlots = GetComponentsInChildren<CraftItemSlot>();

            foreach (var itemSlot in ItemSlots)
            {
                itemSlot.OnItemSlotClickedEvent += OnItemSlotClicked;
                itemSlot.SetItemIcon(null);
                itemSlot.SetLocked(false);
            }
        }

        void OnItemSlotClicked(CraftItemSlot slot) => OnItemSlotClickedEvent?.Invoke(slot);

        public void Refresh()
        {
            var craftableItemIds = ServiceLocator.Get<DataManager>().CraftableItemIds;
            for (int i = 0; i < craftableItemIds.Length && i < ItemSlots.Length; i++)
            {
                var craftableItemId = craftableItemIds[i];
                var itemData = ServiceLocator.Get<DataManager>().ItemDataDictionary[craftableItemId];
                var itemSlot = ItemSlots[i];
                itemSlot.ItemId = craftableItemId;
                itemSlot.SetItemIcon(itemData.Icon);
                itemSlot.SetLocked(!ServiceLocator.Get<CraftSystem>().IsCraftable(craftableItemId));
            }
        }
    }
}