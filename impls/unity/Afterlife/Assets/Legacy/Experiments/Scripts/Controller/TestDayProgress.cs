using UnityEngine;

namespace Afterlife.Controller
{
    public class TestDayProgress : MonoBehaviour
    {
        public UI.Stage.Days DaysView;
        public bool IsExpandable;
        public int TotalDays;
        public float DayDuration;
        public float NightDuration;

        public float TotalDuration;
        public float ElapsedTime;
        public int CurrentDay;

        private void Start()
        {
            TotalDuration = DayDuration + NightDuration;
            ElapsedTime = 0f;
            CurrentDay = 0;

            DaysView.SetExpandable(IsExpandable);
            DaysView.SetTotalDays(TotalDays);
            DaysView.SetDays(CurrentDay + 1);
        }

        void Update()
        {
            ElapsedTime += Time.deltaTime;

            if (ElapsedTime >= TotalDuration)
            {
                ElapsedTime = 0;
                CurrentDay++;
                if (CurrentDay >= TotalDays)
                {
                    enabled = false;
                    return;
                }
                DaysView.SetDays(CurrentDay + 1);
                DaysView.DayProgressView.SetDay();
            }
            else if (ElapsedTime >= DayDuration)
            {
                DaysView.DayProgressView.SetNight();
            }

            DaysView.DayProgressView.SetRatio((CurrentDay + ElapsedTime / TotalDuration) / TotalDays);
        }
    }
}