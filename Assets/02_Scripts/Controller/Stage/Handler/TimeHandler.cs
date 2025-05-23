using System;

namespace Afterlife.Controller
{
    [Serializable]
    public class TimeHandler : Handler
    {
        readonly Model.Stage stage;
        readonly View.Days stageDaysView;

        public float DayDuration;
        public float NightDuration;
        public float ElapsedTimeInDay;
        float totalDuration;

        public event Action<int> OnDayChangedEvent;

        public TimeHandler(Controller controller) : base(controller)
        {
            stage = controller.Game.Stage;
            stageDaysView = controller.StageView.StageDayView;
        }

        public override void SetUp()
        {
            DayDuration = stage.Data.DayDuration;
            NightDuration = stage.Data.NightDuration;
            totalDuration = DayDuration + NightDuration;
            stage.ElapsedTime = 0f;
            stage.Days = 0;
            stage.IsDayTime = true;
            stageDaysView.InitializeProgress(stage.Data.SpawnIntervalPerDay.Length);
            stageDaysView.SetDays(stage.Days + 1);
        }

        public override void Update(float deltaTime)
        {
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
            }
            else if (ElapsedTimeInDay >= DayDuration)
            {
                stage.IsDayTime = false;
                stageDaysView.DayProgressView.SetNight();
            }

            stageDaysView.DayProgressView.SetRatio((stage.Days + ElapsedTimeInDay / totalDuration) / (stage.Data.SpawnIntervalPerDay.Length - 1));
        }
    }
}