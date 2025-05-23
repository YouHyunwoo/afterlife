using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class StageMenu : UIView
    {
        public Button MenuButton;
        public GameObject MenuView;

        public event Action OnMenuButtonClickEvent;

        void Awake()
        {
            MenuButton.onClick.AddListener(OnMenuButtonClick);
        }

        void OnMenuButtonClick() => OnMenuButtonClickEvent?.Invoke();
    }
}