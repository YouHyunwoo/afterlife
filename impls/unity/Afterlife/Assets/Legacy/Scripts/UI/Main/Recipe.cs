using System;
using TMPro;
using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Recipe : View
    {
        [SerializeField] TextMeshProUGUI unlockedCountText;

        public RecipeNode[] RecipeNodes;

        public event Action<RecipeNode> OnRecipeUnlockedEvent;
        public event Action<RecipeNode> OnRecipeInformationShowedEvent;
        public event Action<RecipeNode> OnRecipeInformationHiddenEvent;

        void Awake()
        {
            if (RecipeNodes == null) { return; }

            foreach (var node in RecipeNodes)
            {
                if (node == null) { continue; }
                node.OnUnlocked += OnRecipeUnlocked;
                node.OnInformationShowed += OnRecipeInformationShowed;
                node.OnInformationHidden += OnRecipeInformationHidden;
            }
        }

        void OnRecipeUnlocked(RecipeNode node) => OnRecipeUnlockedEvent?.Invoke(node);
        void OnRecipeInformationShowed(RecipeNode node) => OnRecipeInformationShowedEvent?.Invoke(node);
        void OnRecipeInformationHidden(RecipeNode node) => OnRecipeInformationHiddenEvent?.Invoke(node);

        public void SetUnlockedCount(int unlockedCount, int totalCount)
        {
            if (unlockedCountText == null) { return; }

            unlockedCountText.text = $"{unlockedCount}/{totalCount}";
        }
    }
}
