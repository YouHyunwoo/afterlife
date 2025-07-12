using System;
using Afterlife.Core;
using Afterlife.UI.Title;
using TMPro;
using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Screen : UI.Screen
    {
        [Header("Navigation Bar")]
        public NavigationBar NavigationBarView;

        [Header("Mission")]
        public Mission MissionView;

        [Header("Upgrade")]
        public Upgrade UpgradeView;

        [Header("Popup")]
        public Menu MenuView;
        public UpgradeInformation UpgradeInformationView;
        public Settings SettingsView;

        [Header("Localization")]
        [SerializeField] TextMeshProUGUI navigationBarMissionText;
        [SerializeField] TextMeshProUGUI navigationBarUpgradeText;
        [SerializeField] TextMeshProUGUI missionOpportunitiesLabelText;
        [SerializeField] TextMeshProUGUI missionMissionLabelText;
        [SerializeField] TextMeshProUGUI missionStartMissionButtonText;
        [SerializeField] TextMeshProUGUI menuContinueButtonText;
        [SerializeField] TextMeshProUGUI menuSaveAndQuitButtonText;
        [SerializeField] TextMeshProUGUI settingsText;

        public event Action OnMenuButtonClickedEvent;
        public event Action OnStartMissionButtonClickedEvent;

        void Awake()
        {
            NavigationBarView.OnMenuButtonClickedEvent += OnMenuButtonClickedEvent;
            MissionView.OnStartMissionButtonClickedEvent += OnStartMissionButtonClickedEvent;
        }

        protected override void OnLocalizationChanged()
        {
            navigationBarMissionText.text = Localization.Get("main.navigation-bar.mission");
            navigationBarUpgradeText.text = Localization.Get("main.navigation-bar.upgrade");
            missionOpportunitiesLabelText.text = Localization.Get("main.mission.opportunities.label");
            missionMissionLabelText.text = Localization.Get("main.mission.mission.label");
            missionStartMissionButtonText.text = Localization.Get("main.mission.start-mission-button");
            menuContinueButtonText.text = Localization.Get("main.menu.continue-button");
            menuSaveAndQuitButtonText.text = Localization.Get("main.menu.save-and-quit-button");
            settingsText.text = Localization.Get("title.menu.settings");
        }
    }
}