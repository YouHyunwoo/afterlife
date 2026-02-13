using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class Mission : View
    {
        public Progress MissionProgressView;
        [SerializeField] Button button;

        public event Action OnButtonClickedEvent;

        void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        void OnButtonClicked() => OnButtonClickedEvent?.Invoke();
    }
}