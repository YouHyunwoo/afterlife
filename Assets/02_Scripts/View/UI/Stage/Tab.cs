using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Tab : UIView
    {
        [Header("View")]
        [SerializeField] Button[] buttons;
        [SerializeField] GameObject[] views;

        [Header("Viewer")]
        public int CurrentViewIndex;

        void Awake()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                buttons[i].onClick.AddListener(() => OnButtonClicked(index));
            }

            CurrentViewIndex = 0;
            buttons[CurrentViewIndex].onClick.Invoke();
        }

        void OnButtonClicked(int index)
        {
            if (CurrentViewIndex == index) { return; }

            views[CurrentViewIndex].SetActive(false);
            buttons[CurrentViewIndex].interactable = true;
            CurrentViewIndex = index;
            buttons[CurrentViewIndex].interactable = false;
            views[CurrentViewIndex].SetActive(true);
        }
    }
}