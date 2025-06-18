using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Title
{
    public class Screen : UI.Screen
    {
        [SerializeField] Button newGameButton;
        [SerializeField] Button exitButton;

        public event Action OnNewGameButtonClickedEvent;
        public event Action OnExitButtonClickedEvent;

        void Awake()
        {
            newGameButton.onClick.AddListener(OnNewGameButtonClicked);
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        void OnNewGameButtonClicked() => OnNewGameButtonClickedEvent?.Invoke();
        void OnExitButtonClicked() => OnExitButtonClickedEvent?.Invoke();
    }
}