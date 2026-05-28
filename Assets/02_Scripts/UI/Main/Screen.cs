using Afterlife.Core;
using Afterlife.Domain.Progression.Upgrade.UI;
using Afterlife.UI.Title;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class Screen : UI.Screen
    {
        [Header("Menu")]
        public Button menuButton;

        [Header("Experience")]
        public Amount ExperienceView;

        [Header("Orb")]
        public Orb OrbView;

        [Header("Magic Circle")]
        public MagicCircle MagicCircleView;

        [Header("Soul")]
        public Life LifeView;

        [Header("Guide")]
        public Guide GuideView;

        [Header("Popup")]
        public Tab BaseCampTabView;
        public Menu MenuView;
        public Mission MissionView;
        public UpgradePanel UpgradePanel;
        public Artifact ArtifactView;
        public Recipe RecipeView;
        public UpgradeTooltipView UpgradeTooltipView;
        public Settings SettingsView;

        [Header("Localization")]
        [SerializeField] TextMeshProUGUI menuContinueButtonText;
        [SerializeField] TextMeshProUGUI menuSaveAndQuitButtonText;
        [SerializeField] TextMeshProUGUI settingsText;
        [SerializeField] TextMeshProUGUI missionTitleText;
        [SerializeField] TextMeshProUGUI upgradeTitleText;
        [SerializeField] TextMeshProUGUI skipTutorialText;

        protected override void OnLocalizationChanged()
        {
            menuContinueButtonText.text = LocalizationManager.Get("main.menu.continue-button");
            menuSaveAndQuitButtonText.text = LocalizationManager.Get("main.menu.save-and-quit-button");
            settingsText.text = LocalizationManager.Get("title.menu.settings");
            missionTitleText.text = LocalizationManager.Get("main.mission.text");
            upgradeTitleText.text = LocalizationManager.Get("main.soul-engraving.text");
            skipTutorialText.text = LocalizationManager.Get("main.tutorial.skip-key.text");

            if (ServiceLocator.Get<GameManager>().Game != null)
            {
                GuideView.SetGuideText("opportunity");
            }
        }
    }
}
