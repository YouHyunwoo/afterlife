using System;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Title : UIView
    {
        public Button newGameButton;
        public Button exitButton;

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