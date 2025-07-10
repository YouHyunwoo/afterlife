using System;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class InventorySystem : SystemBase
    {
        [SerializeField] ConstructionSystem constructionSystem;
        [SerializeField] UI.Stage.Inventory inventoryView;
        [SerializeField] UI.Stage.ItemInformation itemInformationView;

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
                if (!isSuccess) { return; }

                slot.SetEquippedIcon(isEquipped);
            }
            else if (itemData.Type == Data.ItemType.Construction)
            {
                inventoryView.Hide();
                itemInformationView.Hide();
                Debug.Log($"Starting construction for item: {itemData.Id}");
                constructionSystem.StartConstruction(itemData, itemData.ConstructionPrefab, itemData.PreviewPrefab);
            }
            else
            {
                // Handle other item types if necessary
            }

            OnItemSlotClickedEvent?.Invoke(slot);
        }

        public void RefreshInventoryView()
        {
            var inventory = ServiceLocator.Get<GameManager>().Game.Player.Inventory;

            for (int j = 0; j < inventoryView.ItemSlots.Length; j++)
            {
                inventoryView.ItemSlots[j].ItemId = null;
                inventoryView.ItemSlots[j].SetItemIcon(null);
                inventoryView.ItemSlots[j].SetEquippedIcon(false);
                inventoryView.ItemSlots[j].SetItemCount(0);
            }

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