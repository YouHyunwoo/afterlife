using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Main : UIView
    {
        [Header("Navigation")]
        public Button menuButton;

        [Header("Mission")]
        public Life lifeView;
        public StageProgress stageProgressView;
        public Button startMissionButton;

        [Header("Power")]
        public Power PowerView;

        [Header("Popup")]
        public Menu MenuView;

        public event Action OnMenuButtonClickedEvent;
        public event Action OnStartMissionButtonClickedEvent;

        void Awake()
        {
            menuButton.onClick.AddListener(OnMenuButtonClicked);
            startMissionButton.onClick.AddListener(OnStartMissionButtonClicked);
        }

        void OnMenuButtonClicked() => OnMenuButtonClickedEvent?.Invoke();
        void OnStartMissionButtonClicked() => OnStartMissionButtonClickedEvent?.Invoke();

        public void SetLifes(int lifes) => lifeView.SetLifes(lifes);
        public void SetStageProgress(int currentStageIndex, int totalStageCount) => stageProgressView.SetStageProgress(currentStageIndex, totalStageCount);
    }
}