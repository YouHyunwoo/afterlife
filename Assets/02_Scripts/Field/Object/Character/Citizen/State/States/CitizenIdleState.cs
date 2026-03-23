using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenIdleState : CitizenState
    {
        private const float WanderCheckInterval = 1f;
        private float _wanderStateTransitionRate;
        private float _nextWanderCheckTime;
        private float _wanderingDisabledUntil;

        public CitizenIdleState(string stateId) : base(stateId)
        {
        }

        protected override void OnInitialize()
        {
            _wanderStateTransitionRate = 0.5f;
        }

        protected override void OnEnter(object[] args)
        {
            _nextWanderCheckTime = Time.time + WanderCheckInterval;
            _wanderingDisabledUntil = 0f;

            if (args != null && args.Length == 1)
            {
                var timeSpan = (TimeSpan)args[0];
                DeactivateWanderingInSeconds(timeSpan);
            }
        }

        protected override void OnUpdate()
        {
            if (Time.time < _wanderingDisabledUntil)
            {
                _nextWanderCheckTime = Time.time + WanderCheckInterval;
                return;
            }

            if (Time.time < _nextWanderCheckTime)
                return;

            _nextWanderCheckTime = Time.time + WanderCheckInterval;

            if (UnityEngine.Random.value < _wanderStateTransitionRate)
            {
                Transit("wander");
            }
        }

        private void DeactivateWanderingInSeconds(TimeSpan timeSpan)
        {
            if (timeSpan <= TimeSpan.Zero)
                return;

            var seconds = (float)timeSpan.TotalSeconds;
            _wanderingDisabledUntil = Time.time + seconds;
            _nextWanderCheckTime = _wanderingDisabledUntil + WanderCheckInterval;
        }
    }
}