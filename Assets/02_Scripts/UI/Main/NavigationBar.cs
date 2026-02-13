using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class NavigationBar : View
    {
        [SerializeField] Button MenuButton;

        public event Action OnMenuButtonClickedEvent;

        void Awake()
        {
            MenuButton.onClick.AddListener(OnMenuButtonClicked);
        }

        void OnMenuButtonClicked() => OnMenuButtonClickedEvent?.Invoke();
    }
}