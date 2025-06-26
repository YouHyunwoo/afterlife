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
            titleText.text = Localization.Get("game-over.title");
            messageText.text = Localization.Get("game-over.message");
            toTitleButtonText.text = Localization.Get("game-over.to-title-button");
        }
    }
}