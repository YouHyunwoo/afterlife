using System;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Menu : UIView
    {
        public Button continueButton;
        public Button saveAndQuitButton;

        public event Action OnContinueButtonClickedEvent;
        public event Action OnSaveAndQuitButtonClickedEvent;

        void Awake()
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            saveAndQuitButton.onClick.AddListener(OnSaveAndQuitButtonClicked);
        }

        void OnContinueButtonClicked() => OnContinueButtonClickedEvent?.Invoke();
        void OnSaveAndQuitButtonClicked() => OnSaveAndQuitButtonClickedEvent?.Invoke();
    }
}