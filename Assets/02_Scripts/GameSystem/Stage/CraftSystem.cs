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

            craftView.OnItemSlotClickedEvent += OnItemSlotClicked;
            craftView.Refresh();
        }

        void OnItemSlotClicked(UI.Stage.CraftItemSlot slot)
        {
            var itemId = slot.ItemId;
            if (string.IsNullOrEmpty(itemId)) { return; }

            if (TryCraft(itemId))
            {
                Debug.Log($"Crafted item: {itemId}");
                craftView.Refresh();
            }
            else
            {
                Debug.LogWarning($"Cannot craft item: {itemId}. Requirements not met.");
            }
        }

        public override void TearDown()
        {
            craftView.OnItemSlotClickedEvent -= OnItemSlotClicked;
            craftView = null;
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

            return true;
        }

        public CraftRequirement[] GetCraftRequirements(string itemId)
        {
            return ServiceLocator.Get<DataManager>().ItemDataDictionary[itemId].CraftRequirements;
        }
    }
}