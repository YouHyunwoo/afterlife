using System;
using Afterlife.Core;
using Afterlife.UI.Title;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class Screen : UI.Screen
    {
        [Header("Menu")]
        [SerializeField] Button MenuButton;

        [Header("Experience")]
        public Amount ExperienceView;

        [Header("Days")]
        public Days DaysView;

        [Header("Skill")]
        public Skill SkillView;

        [Header("Night Overlay")]
        public GameObject NightOverlayView;

        [Header("Popup")]
        public Menu MenuView;
        public SkillInformation SkillInformationView;
        public Inventory InventoryView;
        public Craft CraftView;
        public Reward RewardView;
        public ItemInformation ItemInformationView;
        public Settings SettingsView;

        [Header("Localization")]
        [SerializeField] TextMeshProUGUI menuMissionProgressText;
        [SerializeField] TextMeshProUGUI menuContinueButtonText;
        [SerializeField] TextMeshProUGUI menuGiveUpButtonText;
        [SerializeField] TextMeshProUGUI inventoryTitleText;
        [SerializeField] TextMeshProUGUI craftTitleText;
        [SerializeField] TextMeshProUGUI rewardTitleText;
        [SerializeField] TextMeshProUGUI itemInformationRequirementsText;
        [SerializeField] TextMeshProUGUI settingsText;

        public event Action OnMenuButtonClickedEvent;

        void Awake()
        {
            MenuButton.onClick.AddListener(OnMenuButtonClicked);
        }

        void OnMenuButtonClicked() => OnMenuButtonClickedEvent?.Invoke();

        protected override void OnLocalizationChanged()
        {
            menuMissionProgressText.text = LocalizationManager.Get("stage.popup.menu.mission-progress");
            menuContinueButtonText.text = LocalizationManager.Get("stage.popup.menu.continue-button");
            menuGiveUpButtonText.text = LocalizationManager.Get("stage.popup.menu.giveup-button");
            inventoryTitleText.text = LocalizationManager.Get("stage.popup.inventory.title");
            craftTitleText.text = LocalizationManager.Get("stage.popup.craft.title");
            rewardTitleText.text = LocalizationManager.Get("stage.popup.reward.title");
            itemInformationRequirementsText.text = LocalizationManager.Get("stage.popup.item-information.requirements");
            settingsText.text = LocalizationManager.Get("title.menu.settings");
        }
    }
}