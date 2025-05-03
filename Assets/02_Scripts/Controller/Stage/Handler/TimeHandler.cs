using System;

namespace Afterlife.Controller
{
    [System.Serializable]
    public class TimeHandler : Handler
    {
        readonly Model.Stage stage;

        public float DayDuration;
        public float NightDuration;

        public event Action<int> OnDayChangedEvent;

        public TimeHandler(Controller controller) : base(controller)
        {
            stage = controller.Game.Stage;
        }

        public override void SetUp()
        {
            DayDuration = stage.Data.DayDuration;
            NightDuration = stage.Data.NightDuration;
            stage.ElapsedTime = 0f;
            stage.Days = 0;
            stage.IsDayTime = true;
        }

        public override void Update(float deltaTime)
        {
            stage.ElapsedTime += deltaTime;

            if (stage.IsDayTime && stage.ElapsedTime >= DayDuration)
            {
                stage.ElapsedTime = 0f;
                stage.IsDayTime = false;
            }
            else if (!stage.IsDayTime && stage.ElapsedTime >= NightDuration)
            {
                stage.ElapsedTime = 0f;
                stage.IsDayTime = true;
                stage.Days++;
                OnDayChangedEvent?.Invoke(stage.Days);
            }
        }
    }
}