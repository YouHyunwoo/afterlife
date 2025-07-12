using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class CraftSystem : SystemBase
    {
        [SerializeField] EquipmentSystem equipmentSystem;

        Model.Inventory inventory;
        UI.Stage.Craft craftView;

        public override void SetUp()
        {
            inventory = ServiceLocator.Get<GameManager>().Game.Player.Inventory;

            var stageScreen = ServiceLocator.Get<UIManager>().InGameScreen as UI.Stage.Screen;
            craftView = stageScreen.CraftView;

            foreach (var itemSlot in craftView.ItemSlots)
            {
                itemSlot.OnItemSlotClickedEvent += OnItemSlotClicked;
            }

            RefreshCraftView();
        }

        public override void TearDown()
        {
            foreach (var itemSlot in craftView.ItemSlots)
            {
                itemSlot.OnItemSlotClickedEvent -= OnItemSlotClicked;
            }

            craftView = null;

            inventory = null;
        }

        void OnItemSlotClicked(UI.Stage.ItemSlot slot)
        {
            var itemId = slot.ItemId;
            if (string.IsNullOrEmpty(itemId)) { return; }

            if (!TryCraft(itemId, 1, out var _)) { return; }

            RefreshCraftView();
        }

        public void RefreshCraftView()
        {
            var craftableItemIds = ServiceLocator.Get<DataManager>().CraftableItemIds;

            for (int i = 0; i < craftableItemIds.Length && i < craftView.ItemSlots.Length; i++)
            {
                var craftableItemId = craftableItemIds[i];
                var itemData = ServiceLocator.Get<DataManager>().FindItemData(craftableItemId);
                var itemSlot = craftView.ItemSlots[i];
                itemSlot.ItemId = craftableItemId;
                itemSlot.SetItemIcon(itemData.Icon);
                itemSlot.SetLocked(!IsCraftable(craftableItemId));
            }
        }

        public bool IsCraftable(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) { return false; }

            var itemData = ServiceLocator.Get<DataManager>().FindItemData(itemId);
            var craftRequirements = itemData.CraftRequirements;

            return IsCraftableInternal(craftRequirements);
        }

        bool IsCraftableInternal(Data.CraftRequirement[] craftRequirements)
        {
            if (craftRequirements == null || craftRequirements.Length == 0) { return true; }

            foreach (var craftRequirement in craftRequirements)
            {
                if (!inventory.HasItem(craftRequirement.ItemId, craftRequirement.ItemAmount)) { return false; }
            }

            return true;
        }

        public bool TryCraft(string itemId, int itemAmount, out int craftedAmount)
        {
            if (string.IsNullOrEmpty(itemId)) { craftedAmount = 0; return false; }
            if (itemAmount <= 0) { craftedAmount = 0; return false; }

            var itemData = ServiceLocator.Get<DataManager>().FindItemData(itemId);
            var craftRequirements = itemData.CraftRequirements;

            var craftableAmount = GetCraftableAmount(craftRequirements);
            if (craftableAmount <= 0) { craftedAmount = 0; return false; }

            craftedAmount = Mathf.Min(itemAmount, craftableAmount);

            ConsumeRequiredItemsForCraft(craftRequirements, craftedAmount);
            CraftItem(itemId, craftedAmount);

            if (itemData.Type == Data.ItemType.Equipment)
            {
                equipmentSystem.TryToggleEquipment(itemData, out bool isEquipped);
            }

            return true;
        }

        int GetCraftableAmount(Data.CraftRequirement[] craftRequirements)
        {
            if (craftRequirements == null || craftRequirements.Length == 0) { return int.MaxValue; }

            int minAmount = int.MaxValue;

            foreach (var requirement in craftRequirements)
            {
                if (requirement.ItemAmount <= 0) { continue; }

                var availableAmount = inventory[requirement.ItemId] / requirement.ItemAmount;
                if (availableAmount < minAmount)
                {
                    minAmount = availableAmount;
                }
            }

            return minAmount;
        }

        void ConsumeRequiredItemsForCraft(Data.CraftRequirement[] craftRequirements, int craftableAmount)
        {
            if (craftRequirements == null) { return; }

            foreach (var requirement in craftRequirements)
            {
                inventory.RemoveItem(requirement.ItemId, requirement.ItemAmount * craftableAmount, out _);
            }
        }

        void CraftItem(string itemId, int itemAmount)
        {
            if (itemAmount <= 0) { return; }
            inventory.AddItem(itemId, itemAmount);
        }
    }
}