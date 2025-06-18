using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class Power : View
    {
        public Experience ExperienceView;
        [SerializeField] Button[] playerStatisticsButtons;
        public StoreContent HeroStoreContentView;
        public StoreContent AbilityStoreContentView;
        public StoreContent CraftingStoreContentView;

        public event Action<string, int> OnButtonClickedEvent;

        void Awake()
        {
            for (int i = 0; i < playerStatisticsButtons.Length; i++)
            {
                int index = i;
                playerStatisticsButtons[i].onClick.AddListener(() => OnButtonClicked("player-statistics", index));
            }
        }

        void OnButtonClicked(string category, int index) => OnButtonClickedEvent?.Invoke(category, index);

        public void InitializeUpgradeTree(List<string> upgradeIds)
        {
            var upgradeNodes = GetComponentsInChildren<UpgradeNode>(true);
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.Clear(); // Clear the state of all nodes
            }

            foreach (var upgradeNode in upgradeNodes)
            {
                if (upgradeIds.Contains(upgradeNode.Id))
                {
                    upgradeNode.SetPurchased();
                }
            }
        }
    }
}