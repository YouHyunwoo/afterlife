using TMPro;
using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class Days : View
    {
        public DayProgress DayProgressView;

        [SerializeField] TextMeshProUGUI DaysText;

        public void InitializeProgress(int totalDays)
        {
            DayProgressView.Initialize(totalDays);

            var halfWidth = DayProgressView.RectTransform.sizeDelta.x / 2;
            DaysText.rectTransform.anchoredPosition = new Vector2(-halfWidth - 10, DaysText.rectTransform.anchoredPosition.y);
        }

        public void SetDays(int days) => DaysText.text = $"Day {days}";
    }
}