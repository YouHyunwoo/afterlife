using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class Mission : View
    {
        [SerializeField] Button StartMissionButton;

        public Life LifeView;
        public Progress MissionProgressView;

        public event Action OnStartMissionButtonClickedEvent;

        void Awake()
        {
            StartMissionButton.onClick.AddListener(OnStartMissionButtonClicked);
        }

        void OnStartMissionButtonClicked() => OnStartMissionButtonClickedEvent?.Invoke();
    }
}