using System;
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

        [Header("Day")]
        public Days StageDayView;

        [Header("Skill")]
        public SkillSlotList SkillSlotListView;

        [Header("Night Overlay")]
        public GameObject NightOverlayView;

        [Header("Popup")]
        public Menu MenuView;

        public event Action OnMenuButtonClickedEvent;

        void Awake()
        {
            MenuButton.onClick.AddListener(OnMenuButtonClicked);
        }

        void OnMenuButtonClicked() => OnMenuButtonClickedEvent?.Invoke();

        public void SetExperience(float experience) => ExperienceView.SetAmount(experience);
    }
}