using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class Menu : View
    {
        [SerializeField] Button continueButton;
        [SerializeField] Button saveAndQuitButton;

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