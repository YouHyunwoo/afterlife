using System;
using Afterlife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Title
{
    public class Screen : UI.Screen
    {
        [SerializeField] Button newGameButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button exitButton;

        [Header("Popup")]
        public Settings SettingsView;

        [Header("Localization")]
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI settingsText;
        [SerializeField] TextMeshProUGUI exitText;
        [SerializeField] TextMeshProUGUI settingsTitleText;
        [SerializeField] TextMeshProUGUI languageLabelText;
        [SerializeField] TextMeshProUGUI languageButtonText;

        public event Action OnNewGameButtonClickedEvent;
        public event Action OnSettingsButtonClickedEvent;
        public event Action OnExitButtonClickedEvent;

        void Awake()
        {
            newGameButton.onClick.AddListener(OnNewGameButtonClicked);
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        void OnNewGameButtonClicked() => OnNewGameButtonClickedEvent?.Invoke();
        void OnSettingsButtonClicked() => OnSettingsButtonClickedEvent?.Invoke();
        void OnExitButtonClicked() => OnExitButtonClickedEvent?.Invoke();

        protected override void OnLocalizationChanged()
        {
            titleText.text = Localization.Get("title.menu.new-game");
            settingsText.text = Localization.Get("title.menu.settings");
            exitText.text = Localization.Get("title.menu.exit");
            settingsTitleText.text = Localization.Get("title.menu.settings.title");
            languageLabelText.text = Localization.Get("title.menu.settings.language.label");
            languageButtonText.text = Localization.Get("title.menu.settings.language.button");
        }
    }
}