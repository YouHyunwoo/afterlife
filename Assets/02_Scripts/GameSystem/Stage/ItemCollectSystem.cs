using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class ItemCollectSystem : SystemBase
    {
        [SerializeField] EquipmentSystem equipmentSystem;
        [SerializeField] Transform popupContainerTransform;
        [SerializeField] GameObject popupPrefab;

        Model.Inventory inventory;

        public override void SetUp()
        {
            inventory = ServiceLocator.Get<GameManager>().Game.Player.Inventory;
        }

        public override void TearDown()
        {
            inventory = null;
        }

        public int SampleItems(int itemAmount, float dropRate)
        {
            int collectedAmount = 0;

            for (int i = 0; i < itemAmount; i++)
            {
                if (Random.value <= dropRate)
                {
                    collectedAmount++;
                }
            }

            return collectedAmount;
        }

        public void CollectWithRate(string itemId, int itemAmount)
        {
            if (inventory == null) { return; }
            inventory[itemId] += itemAmount;
            Debug.Log($"Collected {itemAmount} of {itemId}.");

            var itemData = ServiceLocator.Get<DataManager>().FindItemData(itemId);
            if (itemData.Type == Data.ItemType.Equipment)
            {
                equipmentSystem.TryToggleEquipment(itemData, out bool _);
            }

            (ServiceLocator.Get<UI.Stage.Screen>().Controller as UI.Stage.Controller).RefreshInventoryView();
            (ServiceLocator.Get<UI.Stage.Screen>().Controller as UI.Stage.Controller).RefreshCraftView();
        }

        public void ShowPopup(Vector3 position, string itemId, int itemAmount)
        {
            var popup = Instantiate(popupPrefab, popupContainerTransform);
            var itemName = LocalizationManager.Get($"item.{itemId}.name");
            var text = $"{itemName} +{itemAmount}";
            popup.GetComponent<UI.Stage.TextIndicator>().Show(position, text);
        }
    }
}