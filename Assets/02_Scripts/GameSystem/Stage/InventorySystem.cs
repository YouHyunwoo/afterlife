using System;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class InventorySystem : SystemBase
    {
        [SerializeField] UI.Stage.Inventory inventoryView;

        public event Action<UI.Stage.InventoryItemSlot> OnItemSlotClickedEvent;

        public override void SetUp()
        {
            // inventory.OnItemSlotClickedEvent += OnItemSlotClicked;
            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;
            // inventory.OnItemSlotClickedEvent -= OnItemSlotClicked;
        }

        void OnItemSlotClicked(UI.Stage.InventoryItemSlot slot)
        {
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
                inventoryView.ItemSlots[i].SetItemIcon(itemIcon);
                inventoryView.ItemSlots[i].SetItemCount(itemPair.Value);
                i++;
            }
        }
    }
}