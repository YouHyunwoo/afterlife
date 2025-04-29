using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Main : UIView
    {
        [Header("View")]
        [SerializeField] Button[] navigationBarButtons;
        [SerializeField] GameObject[] navigationBarViews;
        [SerializeField] Button menuButton;
        [SerializeField] Button startMissionButton;
        [SerializeField] Life lifeView;
        [SerializeField] StageProgress stageProgressView;

        [Header("Event")]
        [SerializeField] UnityEvent onMenuButtonClicked;
        [SerializeField] UnityEvent onStartMissionButtonClicked;

        public int CurrentViewIndex;

        void Awake()
        {
            for (int i = 0; i < navigationBarButtons.Length; i++)
            {
                int index = i;
                navigationBarButtons[i].onClick.AddListener(() => OnNavigationBarButtonClicked(index));
            }

            menuButton.onClick.AddListener(OnMenuButtonClicked);
            startMissionButton.onClick.AddListener(OnStartMissionButtonClicked);

            CurrentViewIndex = 0;
            navigationBarButtons[CurrentViewIndex].onClick.Invoke();
        }

        void OnNavigationBarButtonClicked(int index)
        {
            if (CurrentViewIndex == index) { return; }
            navigationBarViews[CurrentViewIndex].SetActive(false);
            navigationBarButtons[CurrentViewIndex].interactable = true;
            CurrentViewIndex = index;
            navigationBarButtons[CurrentViewIndex].interactable = false;
            navigationBarViews[CurrentViewIndex].SetActive(true);
        }

        void OnMenuButtonClicked() => onMenuButtonClicked?.Invoke();
        void OnStartMissionButtonClicked() => onStartMissionButtonClicked?.Invoke();

        public void SetLifes(int lifes) => lifeView.SetLifes(lifes);
        public void SetStageProgress(int currentStageIndex, int totalStageCount) => stageProgressView.SetStageProgress(currentStageIndex, totalStageCount);
    }
}