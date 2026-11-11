using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class Orb : View
    {
        [SerializeField] Animator animator;
        [SerializeField] Button button;

        public event Action OnButtonClickedEvent;

        void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        void OnButtonClicked() => OnButtonClickedEvent?.Invoke();
    }
}