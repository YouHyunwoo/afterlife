using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class Screen : UI.Screen
    {
        [Header("Navigation Bar")]
        [SerializeField] Button MenuButton;

        [Header("Mission")]
        public Mission MissionView;
        [SerializeField] Button StartMissionButton;

        [Header("Upgrade")]
        public Power UpgradeView;

        [Header("Popup")]
        public Menu MenuView;
        public UpgradeInformation UpgradeInformationView;

        public event Action OnMenuButtonClickedEvent;
        public event Action OnStartMissionButtonClickedEvent;

        void Awake()
        {
            MenuButton.onClick.AddListener(OnMenuButtonClicked);
            StartMissionButton.onClick.AddListener(OnStartMissionButtonClicked);
        }

        void OnMenuButtonClicked() => OnMenuButtonClickedEvent?.Invoke();
        void OnStartMissionButtonClicked() => OnStartMissionButtonClickedEvent?.Invoke();
    }
}