using System;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class InventorySystem : SystemBase
    {
        [SerializeField] UI.Stage.Inventory inventoryView;

        public event Action<UI.Stage.ItemSlot> OnItemSlotClickedEvent;

        public override void SetUp()
        {
            foreach (var itemSlot in inventoryView.ItemSlots)
            {
                itemSlot.OnItemSlotClickedEvent += OnItemSlotClicked;
            }
            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;
            foreach (var itemSlot in inventoryView.ItemSlots)
            {
                itemSlot.OnItemSlotClickedEvent -= OnItemSlotClicked;
            }
        }

        void OnItemSlotClicked(UI.Stage.ItemSlot slot)
        {
            var itemData = ServiceLocator.Get<DataManager>().ItemDataDictionary[slot.ItemId];
            if (itemData == null)
            {
                Debug.LogWarning($"Item data for {slot.ItemId} not found.");
                return;
            }

            if (itemData.Type == Data.ItemType.Equipment)
            {
                var isSuccess = ServiceLocator.Get<EquipmentSystem>().TryToggleEquipment(itemData, out bool isEquipped);
                if (isSuccess)
                {
                    slot.SetEquippedIcon(isEquipped);
                }
            }

            OnItemSlotClickedEvent?.Invoke(slot);
        }

        public void RefreshInventoryView()
        {
            var inventory = ServiceLocator.Get<GameManager>().Game.Player.Inventory;

            var i = 0;
            foreach (var itemPair in inventory)
            {
                var itemData = ServiceLocator.Get<DataManager>().ItemDataDictionary[itemPair.Key];
                if (itemData == null)
                {
                    Debug.LogWarning($"Item data for {itemPair.Key} not found.");
                    continue;
                }
                var itemIcon = itemData.Icon;
                inventoryView.ItemSlots[i].ItemId = itemPair.Key;
                inventoryView.ItemSlots[i].SetItemIcon(itemIcon);
                inventoryView.ItemSlots[i].SetEquippedIcon(itemData.Type == Data.ItemType.Equipment && ServiceLocator.Get<GameManager>().Game.Player.Equipment.Contains(itemPair.Key));
                inventoryView.ItemSlots[i].SetItemCount(itemPair.Value);
                i++;
            }
        }
    }
}