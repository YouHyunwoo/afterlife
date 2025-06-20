using System;
using UnityEngine.UI;

namespace Afterlife.UI.Title
{
    public class Settings : View
    {
        public Button LanguageButton;

        public event Action OnLanguageButtonClickedEvent;

        void Awake()
        {
            LanguageButton.onClick.AddListener(OnLanguageButtonClicked);
        }

        void OnLanguageButtonClicked() => OnLanguageButtonClickedEvent?.Invoke();
    }
}