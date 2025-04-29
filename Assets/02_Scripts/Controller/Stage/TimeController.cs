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
                OnDayChanged?.Invoke(Days);
                onDayChangedEvent?.Invoke(Days);
            }
        }

        public void Initialize(float dayDuration, float nightDuration)
        {
            DayDuration = dayDuration;
            NightDuration = nightDuration;
            ElapsedTime = 0f;
            Days = 0;
            enabled = true;
        }
    }
}