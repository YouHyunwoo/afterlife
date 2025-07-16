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
        [SerializeField] TextMeshProUGUI soundsCategoryText;
        [SerializeField] TextMeshProUGUI masterLabelText;
        [SerializeField] TextMeshProUGUI bgmLabelText;
        [SerializeField] TextMeshProUGUI sfxLabelText;
        [SerializeField] TextMeshProUGUI interfacesCategoryText;
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
            titleText.text = LocalizationManager.Get("title.menu.new-game");
            settingsText.text = LocalizationManager.Get("title.menu.settings");
            exitText.text = LocalizationManager.Get("title.menu.exit");
            settingsTitleText.text = LocalizationManager.Get("title.menu.settings.title");
            soundsCategoryText.text = LocalizationManager.Get("title.menu.settings.sounds.category");
            masterLabelText.text = LocalizationManager.Get("title.menu.settings.sounds.master.label");
            bgmLabelText.text = LocalizationManager.Get("title.menu.settings.sounds.bgm.label");
            sfxLabelText.text = LocalizationManager.Get("title.menu.settings.sounds.sfx.label");
            interfacesCategoryText.text = LocalizationManager.Get("title.menu.settings.interfaces.category");
            languageLabelText.text = LocalizationManager.Get("title.menu.settings.interfaces.language.label");
            languageButtonText.text = LocalizationManager.Get("title.menu.settings.interfaces.language.button");
        }
    }
}