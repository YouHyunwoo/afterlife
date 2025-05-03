using System;
using UnityEngine;
using UnityEngine.Events;

namespace Afterlife.Controller
{
    public class TimeController : MonoBehaviour
    {
        [Header("Viewer")]
        public float ElapsedTime;
        public int Days;
        public bool IsDayTime;
        public float DayDuration;
        public float NightDuration;

        [Header("Event")]
        [SerializeField] UnityEvent<int> onDayChangedEvent;

        public event Action<int> OnDayChanged;

        Model.Stage stage;

        public void SetUp() {
            stage = Controller.Instance.Game.Stage;
            DayDuration = stage.Data.DayDuration;
            NightDuration = stage.Data.NightDuration;
            ElapsedTime = 0f;
            Days = 0;
            IsDayTime = true;
            enabled = true;
        }

        void Start()
        {
            enabled = false;
        }

        void Update()
        {
            ElapsedTime += Time.deltaTime;
            IsDayTime = ElapsedTime < DayDuration;
            if (ElapsedTime >= DayDuration + NightDuration)
            {
                ElapsedTime = 0f;
                Days++;
                stage.ElapsedTime = ElapsedTime;
                OnDayChanged?.Invoke(Days);
                onDayChangedEvent?.Invoke(Days);
            }
        }
    }
}