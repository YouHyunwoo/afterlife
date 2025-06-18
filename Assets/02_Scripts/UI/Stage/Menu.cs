using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class Menu : View
    {
        [SerializeField] Button continueButton;
        [SerializeField] Button giveUpButton;

        public Progress MissionProgressView;

        public event Action OnContinueButtonClickedEvent;
        public event Action OnGiveUpButtonClickedEvent;

        void Awake()
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            giveUpButton.onClick.AddListener(OnGiveUpButtonClicked);
        }

        void OnContinueButtonClicked() => OnContinueButtonClickedEvent?.Invoke();
        void OnGiveUpButtonClicked() => OnGiveUpButtonClickedEvent?.Invoke();
    }
}