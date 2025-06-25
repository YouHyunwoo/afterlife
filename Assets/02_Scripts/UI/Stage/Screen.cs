using System;
using Afterlife.Core;
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
        public Craft CraftView;
        public Reward RewardView;

        [Header("Localization")]
        [SerializeField] TextMeshProUGUI menuContinueButtonText;
        [SerializeField] TextMeshProUGUI menuGiveUpButtonText;

        public event Action OnMenuButtonClickedEvent;

        void Awake()
        {
            MenuButton.onClick.AddListener(OnMenuButtonClicked);
        }

        void OnMenuButtonClicked() => OnMenuButtonClickedEvent?.Invoke();

        protected override void OnLocalizationChanged()
        {
            menuContinueButtonText.text = Localization.Get("stage.menu.continue-button");
            menuGiveUpButtonText.text = Localization.Get("stage.menu.giveup-button");
        }
    }
}