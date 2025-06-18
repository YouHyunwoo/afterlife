using System;
using Afterlife.Core;

namespace Afterlife.GameSystem.Stage
{
    public class TimeSystem : SystemBase
    {
        Model.Stage stage;
        UI.Stage.Days stageDaysView;

        public float DayDuration;
        public float NightDuration;
        public float ElapsedTimeInDay;
        float totalDuration;

        public event Action<int> OnDayChangedEvent;
        public float TimeScale { get; private set; } = 1f;

        public override void SetUp()
        {
            stage = ServiceLocator.Get<StageManager>().Stage;
            var stageScreen = ServiceLocator.Get<UIManager>().InGameScreen as UI.Stage.Screen;
            stageDaysView = stageScreen.DaysView;

            DayDuration = stage.Data.DayDuration;
            NightDuration = stage.Data.NightDuration;
            totalDuration = DayDuration + NightDuration;
            stage.ElapsedTime = 0f;
            stage.Days = 0;
            stage.IsDayTime = true;
            stageDaysView.SetTotalDays(stage.Data.SpawnIntervalPerDay.Length);
            stageDaysView.SetDays(stage.Days + 1);

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            stage = null;
            stageDaysView = null;
        }

        void Update()
        {
            var deltaTime = UnityEngine.Time.deltaTime * TimeScale;

            stage.ElapsedTime += deltaTime;
            ElapsedTimeInDay += deltaTime;

            if (ElapsedTimeInDay >= totalDuration)
            {
                ElapsedTimeInDay = 0f;
                stage.Days++;
                stage.IsDayTime = true;
                stageDaysView.SetDays(stage.Days + 1);
                stageDaysView.DayProgressView.SetDay();
                OnDayChangedEvent?.Invoke(stage.Days);
                return;
            }
            else if (ElapsedTimeInDay >= DayDuration)
            {
                stage.IsDayTime = false;
                stageDaysView.DayProgressView.SetNight();
            }

            stageDaysView.DayProgressView.SetRatio((stage.Days + ElapsedTimeInDay / totalDuration) / (stage.Data.SpawnIntervalPerDay.Length));
        }

        // public void SetTimeScale(float scale)
        // {
        //     TimeScale = scale;
        //     UnityEngine.Time.timeScale = scale;
        // }
    }
}