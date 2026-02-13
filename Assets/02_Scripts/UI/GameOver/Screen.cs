using System;
using Afterlife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.GameOver
{
    public class Screen : UI.Screen
    {
        [SerializeField] Button toTitleButton;

        [Header("Localization")]
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI messageText;
        [SerializeField] TextMeshProUGUI toTitleButtonText;

        public event Action OnToTitleButtonClickedEvent;

        void Awake()
        {
            toTitleButton.onClick.AddListener(OnToTitleButtonClicked);
        }

        void OnToTitleButtonClicked() => OnToTitleButtonClickedEvent?.Invoke();

        protected override void OnLocalizationChanged()
        {
            titleText.text = LocalizationManager.Get("game-over.title");
            messageText.text = LocalizationManager.Get("game-over.message");
            toTitleButtonText.text = LocalizationManager.Get("game-over.to-title-button");
        }
    }
}