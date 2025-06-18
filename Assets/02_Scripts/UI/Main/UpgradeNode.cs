using System;
using Afterlife.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class UpgradeNode : View
    {
        public enum UpgradeState { Locked, Unlocked, Purchased }

        [SerializeField] EventTrigger eventTrigger;
        [SerializeField] Button button;
        [SerializeField] Image lockedOverlayImage;
        public string Id;
        public string Name;
        public string Description;
        public int Cost;
        public UpgradeNode[] Prerequisites;
        public UpgradeNode[] NextNodes;
        public UpgradeState State;

        public event Action<UpgradeNode> OnInformationShowed;
        public event Action<UpgradeNode> OnInformationHidden;

        void Awake()
        {
            var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryEnter.callback.AddListener((data) => { OnPointerEnter((PointerEventData)data); });
            eventTrigger.triggers.Add(entryEnter);
            var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener((data) => { OnPointerExit((PointerEventData)data); });
            eventTrigger.triggers.Add(entryExit);

            button.onClick.AddListener(OnButtonClick);
        }

        void OnPointerEnter(PointerEventData eventData) => OnInformationShowed?.Invoke(this);
        void OnPointerExit(PointerEventData eventData) => OnInformationHidden?.Invoke(this);

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
                Debug.Log($"Experience after purchase: {player.Experience}");
                State = UpgradeState.Purchased;
                game.Upgrade.ApplyUpgrade(Id);
                OnUpgradePurchased();
                UnlockNextUpgrades();
            }
        }

        void UnlockNextUpgrades()
        {
            foreach (var upgrade in NextNodes)
            {
                if (upgrade.State == UpgradeState.Locked)
                {
                    bool allPrerequisitesMet = true;
                    foreach (var prerequisite in upgrade.Prerequisites)
                    {
                        if (prerequisite.State != UpgradeState.Purchased)
                        {
                            allPrerequisitesMet = false;
                            break;
                        }
                    }
                    if (allPrerequisitesMet)
                    {
                        upgrade.Unlock();
                    }
                }
            }
        }

        void Start()
        {
            if (Prerequisites.Length == 0)
            {
                State = UpgradeState.Unlocked; // No prerequisites means it's available from the start
                lockedOverlayImage.gameObject.SetActive(false); // Hide the locked cover image
            }
            else
            {
                State = UpgradeState.Locked; // Default state is locked if prerequisites exist
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

        protected virtual void OnUpgradePurchased()
        {
            Debug.Log($"Upgrade {Name} has been purchased.");
        }
    }
}