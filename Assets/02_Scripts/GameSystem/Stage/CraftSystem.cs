using Afterlife.Core;
using Afterlife.Data;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class CraftSystem : SystemBase
    {
        UI.Stage.Craft craftView;

        public override void SetUp()
        {
            var stageScreen = ServiceLocator.Get<UIManager>().InGameScreen as UI.Stage.Screen;
            craftView = stageScreen.CraftView;

            foreach (var itemSlot in craftView.ItemSlots)
            {
                itemSlot.OnItemSlotClickedEvent += OnItemSlotClicked;
            }

            RefreshCraftView();
        }

        void OnItemSlotClicked(UI.Stage.ItemSlot slot)
        {
            var itemId = slot.ItemId;
            if (string.IsNullOrEmpty(itemId)) { return; }

            if (TryCraft(itemId))
            {
                Debug.Log($"Crafted item: {itemId}");
                RefreshCraftView();
            }
            else
            {
                Debug.LogWarning($"Cannot craft item: {itemId}. Requirements not met.");
            }
        }

        public override void TearDown()
        {
            foreach (var itemSlot in craftView.ItemSlots)
            {
                itemSlot.OnItemSlotClickedEvent -= OnItemSlotClicked;
            }

            craftView = null;
        }

        public void RefreshCraftView()
        {
            var craftableItemIds = ServiceLocator.Get<DataManager>().CraftableItemIds;
            for (int i = 0; i < craftableItemIds.Length && i < craftView.ItemSlots.Length; i++)
            {
                var craftableItemId = craftableItemIds[i];
                var itemData = ServiceLocator.Get<DataManager>().ItemDataDictionary[craftableItemId];
                var itemSlot = craftView.ItemSlots[i];
                itemSlot.ItemId = craftableItemId;
                itemSlot.SetItemIcon(itemData.Icon);
                itemSlot.SetLocked(!ServiceLocator.Get<CraftSystem>().IsCraftable(craftableItemId));
                Debug.Log(ServiceLocator.Get<CraftSystem>().IsCraftable(craftableItemId)
                    ? $"Item {craftableItemId} is craftable."
                    : $"Item {craftableItemId} is not craftable.");
            }
        }

        public bool IsCraftable(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) { return false; }

            var requiredItems = GetCraftRequirements(itemId);
            return IsCraftableInternal(requiredItems);
        }

        bool IsCraftableInternal(CraftRequirement[] craftRequirements)
        {
            var inventory = ServiceLocator.Get<GameManager>().Game.Player.Inventory;

            foreach (var craftRequirement in craftRequirements)
            {
                if (!inventory.ContainsKey(craftRequirement.ItemId)) { return false; }
                var itemAmount = inventory[craftRequirement.ItemId];
                if (itemAmount < craftRequirement.Amount) { return false; }
            }

            return true;
        }

        public bool TryCraft(string itemId)
        {
            var craftRequirements = GetCraftRequirements(itemId);
            var inventory = ServiceLocator.Get<GameManager>().Game.Player.Inventory;

            if (!IsCraftableInternal(craftRequirements)) { return false; }

            // Remove the required items from the inventory
            foreach (var craftRequirement in craftRequirements)
            {
                inventory[craftRequirement.ItemId] -= craftRequirement.Amount;
                if (inventory[craftRequirement.ItemId] <= 0)
                {
                    inventory.Remove(craftRequirement.ItemId);
                }
            }

            // Add the crafted item to the inventory
            if (inventory.ContainsKey(itemId))
            {
                inventory[itemId] += 1;
            }
            else
            {
                inventory[itemId] = 1;
            }

            var itemData = ServiceLocator.Get<DataManager>().ItemDataDictionary[itemId];
            if (itemData.Type == ItemType.Equipment)
            {
                ServiceLocator.Get<EquipmentSystem>().TryToggleEquipment(itemData, out bool isEquipped);
                ServiceLocator.Get<InventorySystem>().RefreshInventoryView();
            }

            return true;
        }

        public CraftRequirement[] GetCraftRequirements(string itemId)
        {
            return ServiceLocator.Get<DataManager>().ItemDataDictionary[itemId].CraftRequirements;
        }
    }
}