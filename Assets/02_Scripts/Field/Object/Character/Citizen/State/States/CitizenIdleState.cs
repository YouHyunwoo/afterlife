using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenIdleState : CitizenState
    {
        private const float WanderCheckInterval = 1f;
        private float _wanderStateTransitionRate;
        private float _nextWanderCheckTime;

        public CitizenIdleState(string stateId) : base(stateId)
        {
        }

        protected override void OnInitialize()
        {
            _wanderStateTransitionRate = 0.1f;
            _nextWanderCheckTime = Time.time + WanderCheckInterval;
        }

        protected override void OnUpdate()
        {
            if (Time.time < _nextWanderCheckTime)
                return;

            _nextWanderCheckTime = Time.time + WanderCheckInterval;

            if (Random.value < _wanderStateTransitionRate)
            {
                Transit("wander");
            }
        }
    }
}