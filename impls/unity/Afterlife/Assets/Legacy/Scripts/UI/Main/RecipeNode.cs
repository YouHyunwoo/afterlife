using System;
using Afterlife.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class RecipeNode : View, IPointerEnterHandler, IPointerExitHandler
    {
        public enum RecipeState { Locked, Unlocked, Purchased }

        [SerializeField] Button button;
        [SerializeField] Image iconImage;
        [SerializeField] Image borderImage;
        [SerializeField] Image lockedOverlayImage;

        public Data.RecipeUnlock RecipeData;
        public RecipeNode[] Prerequisites;
        public RecipeNode[] NextNodes;
        public RecipeState State;

        public event Action<RecipeNode> OnUnlocked;
        public event Action<RecipeNode> OnInformationShowed;
        public event Action<RecipeNode> OnInformationHidden;

        void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        void OnButtonClicked()
        {
            if (State != RecipeState.Unlocked) { return; }

            TryPurchase();
        }

        void TryPurchase()
        {
            if (RecipeData == null) { return; }

            var player = ServiceLocator.Get<GameManager>().Game.Player;
            if (player.UnlockedRecipeIds.Contains(RecipeData.Id)) { return; }
            if (!player.SpendExperience(RecipeData.Cost)) { return; }

            player.UnlockedRecipeIds.Add(RecipeData.Id);
            SetPurchased();
            OnUnlocked?.Invoke(this);
        }

        public void Refresh()
        {
            SetIcon();

            var player = ServiceLocator.Get<GameManager>().Game.Player;
            if (RecipeData != null && player.UnlockedRecipeIds.Contains(RecipeData.Id))
            {
                SetPurchased();
                return;
            }

            if (ArePrerequisitesPurchased())
            {
                Unlock();
            }
            else
            {
                Lock();
            }
        }

        void SetIcon()
        {
            if (iconImage == null || RecipeData == null || string.IsNullOrEmpty(RecipeData.ItemId)) { return; }

            var itemData = ServiceLocator.Get<DataManager>().FindItemData(RecipeData.ItemId);
            iconImage.sprite = itemData.Icon;
            iconImage.enabled = itemData.Icon != null;
        }

        bool ArePrerequisitesPurchased()
        {
            if (Prerequisites == null || Prerequisites.Length == 0) { return true; }

            foreach (var prerequisite in Prerequisites)
            {
                if (prerequisite.State != RecipeState.Purchased) { return false; }
            }

            return true;
        }

        void Lock()
        {
            State = RecipeState.Locked;
            if (lockedOverlayImage != null) { lockedOverlayImage.gameObject.SetActive(true); }
            if (borderImage != null) { borderImage.color = new Color(0.3f, 0.3f, 0.3f); }
        }

        public void Unlock()
        {
            State = RecipeState.Unlocked;
            if (lockedOverlayImage != null) { lockedOverlayImage.gameObject.SetActive(false); }
            if (borderImage != null) { borderImage.color = Color.gray; }
        }

        public void SetPurchased()
        {
            State = RecipeState.Purchased;
            if (lockedOverlayImage != null) { lockedOverlayImage.gameObject.SetActive(false); }
            if (borderImage != null) { borderImage.color = Color.white; }

            if (NextNodes == null) { return; }
            foreach (var nextNode in NextNodes)
            {
                nextNode.Refresh();
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) => OnInformationShowed?.Invoke(this);
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => OnInformationHidden?.Invoke(this);
    }
}
