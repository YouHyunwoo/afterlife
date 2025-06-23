using System;
using Afterlife.Core;

namespace Afterlife.GameSystem.Stage
{
    public class TimeSystem : SystemBase
    {
        Model.Stage stage;
        UI.Stage.Days stageDaysView;

        public Data.Day[] DayDataArray;
        public Data.Day CurrentDayData;
        public float ElapsedTimeEachDay;
        public float DurationEachDay;

        public event Action<int> OnDayChangedEvent;
        public float TimeScale { get; private set; } = 1f;

        public override void SetUp()
        {
            stage = ServiceLocator.Get<StageManager>().Stage;
            DayDataArray = stage.Data.DayDataArray;

            SetDay(0);
            stage.ElapsedTime = 0f;

            var stageScreen = ServiceLocator.Get<UIManager>().InGameScreen as UI.Stage.Screen;
            stageDaysView = stageScreen.DaysView;
            stageDaysView.SetTotalDays(DayDataArray.Length);
            stageDaysView.SetDays(stage.Days + 1);

            enabled = true;
        }

        void SetDay(int dayIndex)
        {
            stage.Days = dayIndex;
            stage.IsDayTime = true;

            CurrentDayData = dayIndex < DayDataArray.Length ? DayDataArray[stage.Days] : null;
            ElapsedTimeEachDay = 0f;
            DurationEachDay = dayIndex < DayDataArray.Length ? CurrentDayData.DayDuration + CurrentDayData.NightDuration : 0f;
        }

        void NextDay() => SetDay(stage.Days + 1);

        public override void TearDown()
        {
            enabled = false;

            stage.Days = 0;
            stage.IsDayTime = true;
            stage.ElapsedTime = 0f;
            CurrentDayData = null;
            ElapsedTimeEachDay = 0f;
            DurationEachDay = 0f;

            stage = null;
            stageDaysView = null;
        }

        void Update()
        {
            var deltaTime = UnityEngine.Time.deltaTime * TimeScale;

            if (stage.Days >= DayDataArray.Length)
            {
                enabled = false;
                return;
            }

            stage.ElapsedTime += deltaTime;
            var nextElapsedTimeInDay = ElapsedTimeEachDay + deltaTime;

            if (IsNight(ElapsedTimeEachDay, nextElapsedTimeInDay))
            {
                stage.IsDayTime = false;
                stageDaysView.DayProgressView.SetNight();
            }
            else if (IsNextDay(ElapsedTimeEachDay, nextElapsedTimeInDay))
            {
                NextDay();
                stageDaysView.SetDays(stage.Days + 1);
                stageDaysView.DayProgressView.SetDay();
                OnDayChangedEvent?.Invoke(stage.Days);
                return;
            }

            ElapsedTimeEachDay = nextElapsedTimeInDay;
            stageDaysView.DayProgressView.SetRatio((stage.Days + ElapsedTimeEachDay / DurationEachDay) / DayDataArray.Length);
        }

        bool IsNextDay(float elapsedTimeInDay, float nextElapsedTimeInDay)
        {
            return elapsedTimeInDay < DurationEachDay && nextElapsedTimeInDay >= DurationEachDay;
        }

        bool IsNight(float elapsedTimeInDay, float nextElapsedTimeInDay)
        {
            return elapsedTimeInDay < CurrentDayData.DayDuration && nextElapsedTimeInDay >= CurrentDayData.DayDuration;
        }

        // public void SetTimeScale(float scale)
        // {
        //     TimeScale = scale;
        //     UnityEngine.Time.timeScale = scale;
        // }
    }
}