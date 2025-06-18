using System;
using Afterlife.UI.Main;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class Menu : View
    {
        public StageProgress StageProgressView;
        [SerializeField] Button continueButton;
        [SerializeField] Button giveUpButton;

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