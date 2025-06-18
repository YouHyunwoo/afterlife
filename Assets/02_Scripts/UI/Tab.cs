using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI
{
    public class Tab : View
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

        void OnButtonClicked(int index) => SetView(index);

        public void SetView(int index)
        {
            if (index < 0 || index >= views.Length) { return; }

            if (CurrentViewIndex == index) { return; }

            views[CurrentViewIndex].SetActive(false);
            buttons[CurrentViewIndex].interactable = true;
            CurrentViewIndex = index;
            buttons[CurrentViewIndex].interactable = false;
            views[CurrentViewIndex].SetActive(true);
        }
    }
}