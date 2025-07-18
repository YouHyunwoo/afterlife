using System;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class ItemUsageSystem : SystemBase
    {
        [SerializeField] EquipmentSystem equipmentSystem;
        [SerializeField] ConstructionSystem constructionSystem;
        [SerializeField] UI.Stage.Inventory inventoryView;
        [SerializeField] UI.Stage.ItemInformation itemInformationView;

        Model.Inventory inventory;

        public event Action<UI.Stage.ItemSlot> OnItemSlotClickedEvent;

        public override void SetUp()
        {
            inventory = ServiceLocator.Get<GameManager>().Game.Player.Inventory;

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

            inventory = null;
        }

        void OnItemSlotClicked(UI.Stage.ItemSlot slot)
        {
            var itemId = slot.ItemId;
            if (string.IsNullOrEmpty(itemId)) { return; }

            var itemData = ServiceLocator.Get<DataManager>().FindItemData(itemId);

            switch (itemData.Type)
            {
                case Data.ItemType.Equipment:
                    {
                        var isSuccess = equipmentSystem.TryToggleEquipment(itemData, out bool isEquipped);
                        if (!isSuccess) { return; }

                        slot.SetEquippedIcon(isEquipped);
                        break;
                    }
                case Data.ItemType.Construction:
                    {
                        inventoryView.Hide();
                        itemInformationView.Hide();
                        Debug.Log($"Starting construction for item: {itemData.Id}");
                        constructionSystem.StartConstruction(itemData, itemData.ConstructionPrefab, itemData.PreviewPrefab);
                        break;
                    }
            }

            OnItemSlotClickedEvent?.Invoke(slot);
        }
    }
}