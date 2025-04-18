using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Title : UIView
    {
        [SerializeField] GameObject mainViewObject;
        [SerializeField] Button newGameButton;
        [SerializeField] Button exitButton;

        void Awake()
        {
            newGameButton.onClick.AddListener(OnNewGameButtonClicked);
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        void OnNewGameButtonClicked()
        {
            mainViewObject.SetActive(true);
            Hide();
        }

        void OnExitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}