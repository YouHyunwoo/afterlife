using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class Menu : View
    {
        [SerializeField] Button continueButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button giveUpButton;

        public Progress MissionProgressView;

        public event Action OnContinueButtonClickedEvent;
        public event Action OnSettingsButtonClickedEvent;
        public event Action OnGiveUpButtonClickedEvent;

        void Awake()
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            giveUpButton.onClick.AddListener(OnGiveUpButtonClicked);
        }

        void OnContinueButtonClicked() => OnContinueButtonClickedEvent?.Invoke();
        void OnSettingsButtonClicked() => OnSettingsButtonClickedEvent?.Invoke();
        void OnGiveUpButtonClicked() => OnGiveUpButtonClickedEvent?.Invoke();
    }
}