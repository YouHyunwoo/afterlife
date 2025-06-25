using System.Collections.Generic;
using Afterlife.Core;
using Afterlife.UI.Stage;
using TMPro;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class ItemCollectSystem : SystemBase
    {
        [SerializeField] Transform popupContainerTransform;
        [SerializeField] GameObject popupPrefab;

        Dictionary<string, int> inventory;

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
            if (!inventory.ContainsKey(itemId)) { inventory[itemId] = 0; }
            inventory[itemId] += itemAmount;
            Debug.Log(itemId + " : " + inventory[itemId]);
            Debug.Log($"Collected {itemAmount} of {itemId}.");
        }

        public void ShowPopup(Vector3 position, string itemId, int itemAmount)
        {
            var popup = Instantiate(popupPrefab, popupContainerTransform);
            var itemName = Localization.Get($"item.{itemId}.name");
            var text = $"{itemName} +{itemAmount}";
            popup.GetComponent<TextIndicator>().Show(position, text);
        }
    }
}