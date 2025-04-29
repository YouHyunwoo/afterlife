using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Title : UIView
    {
        [Header("View")]
        [SerializeField] Main mainView;
        [SerializeField] Button newGameButton;
        [SerializeField] Button exitButton;

        [Header("Event")]
        [SerializeField] UnityEvent onNewGameButtonClickedEvent;
        [SerializeField] UnityEvent onExitButtonClickedEvent;

        void Awake()
        {
            newGameButton.onClick.AddListener(OnNewGameButtonClicked);
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        void OnNewGameButtonClicked() => onNewGameButtonClickedEvent?.Invoke();
        void OnExitButtonClicked() => onExitButtonClickedEvent?.Invoke();
    }
}