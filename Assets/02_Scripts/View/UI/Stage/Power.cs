using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Power : UIView
    {
        public Experience ExperienceView;
        [SerializeField] Button[] playerStatisticsButtons;

        public event Action<string, int> OnButtonClickedEvent;

        void Awake()
        {
            for (int i = 0; i < playerStatisticsButtons.Length; i++)
            {
                int index = i;
                playerStatisticsButtons[i].onClick.AddListener(() => OnButtonClicked("player-statistics", index));
            }
        }

        void OnButtonClicked(string category, int index) => OnButtonClickedEvent?.Invoke(category, index);
    }
}