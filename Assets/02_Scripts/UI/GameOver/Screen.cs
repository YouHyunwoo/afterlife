using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.GameOver
{
    public class Screen : UI.Screen
    {
        [SerializeField] Button toTitleButton;

        public event Action OnToTitleButtonClickedEvent;

        void Awake()
        {
            toTitleButton.onClick.AddListener(OnToTitleButtonClicked);
        }

        void OnToTitleButtonClicked()
        {
            OnToTitleButtonClickedEvent?.Invoke();
        }
    }
}