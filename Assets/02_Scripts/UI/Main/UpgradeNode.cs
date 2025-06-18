using System;
using Afterlife.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class UpgradeNode : View, IPointerEnterHandler, IPointerExitHandler
    {
        public enum UpgradeState { Locked, Unlocked, Purchased }

        [SerializeField] Button button;
        [SerializeField] Image borderImage;
        [SerializeField] Image lockedOverlayImage;
        public string Id;
        public string Name;
        public string Description;
        public int Cost;
        public UpgradeNode[] Prerequisites;
        public UpgradeNode[] NextNodes;
        public UpgradeState State;

        public event Action<UpgradeNode> OnPurchased;
        public event Action<UpgradeNode> OnInformationShowed;
        public event Action<UpgradeNode> OnInformationHidden;

        void Awake()
        {
            button.onClick.AddListener(OnButtonClick);
        }

        void OnButtonClick()
        {
            if (State == UpgradeState.Unlocked)
            {
                TryPurchase();
            }
        }

        void TryPurchase()
        {
            var gameManager = ServiceLocator.Get<GameManager>();
            var game = gameManager.Game;
            var player = game.Player;
            if (player.Experience >= Cost)
            {
                player.Experience -= Cost; // Example of deducting cost from player's experience
                player.Upgrades.Add(Id); // Add the upgrade ID to the player's upgrades
                game.Upgrade.ApplyUpgrade(Id);
                SetPurchased();
                OnPurchased?.Invoke(this);
            }
        }

        public void UnlockNextUpgrades()
        {
            foreach (var nextNode in NextNodes)
            {
                if (nextNode.State == UpgradeState.Locked)
                {
                    bool allPrerequisitesMet = true;
                    foreach (var prerequisite in nextNode.Prerequisites)
                    {
                        if (prerequisite.State != UpgradeState.Purchased)
                        {
                            allPrerequisitesMet = false;
                            break;
                        }
                    }
                    if (allPrerequisitesMet)
                    {
                        nextNode.Unlock();
                    }
                }
            }
        }

        public void Unlock()
        {
            if (State == UpgradeState.Locked)
            {
                State = UpgradeState.Unlocked;
                lockedOverlayImage.gameObject.SetActive(false); // Hide the locked cover image
            }
        }

        public void Clear()
        {
            if (Prerequisites.Length == 0)
            {
                State = UpgradeState.Unlocked; // No prerequisites means it's available from the start
                lockedOverlayImage.gameObject.SetActive(false); // Hide the locked cover image
            }
            else
            {
                State = UpgradeState.Locked; // Default state is locked if prerequisites exist
                lockedOverlayImage.gameObject.SetActive(true); // Show the locked cover image
            }

            borderImage.color = new Color(0.3f, 0.3f, 0.3f);
        }

        public void SetPurchased()
        {
            State = UpgradeState.Purchased;
            borderImage.color = Color.white; // Change border color to indicate purchase
            lockedOverlayImage.gameObject.SetActive(false); // Hide the locked cover image
            UnlockNextUpgrades();
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) => OnInformationShowed?.Invoke(this);
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => OnInformationHidden?.Invoke(this);
    }
}