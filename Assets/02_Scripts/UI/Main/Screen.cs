using System;
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

        public event Action OnMenuButtonClickedEvent;
        public event Action OnStartMissionButtonClickedEvent;

        void Awake()
        {
            NavigationBarView.OnMenuButtonClickedEvent += OnMenuButtonClickedEvent;
            MissionView.OnStartMissionButtonClickedEvent += OnStartMissionButtonClickedEvent;
        }
    }
}