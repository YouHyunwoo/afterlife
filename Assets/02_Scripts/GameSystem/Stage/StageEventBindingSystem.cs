using System;
using Afterlife.GameSystem.Stage.Field;

namespace Afterlife.GameSystem.Stage
{
    public class StageEventBindingSystem
    {
        public void Bind(
            TimeSystem timeSystem,
            MissionSystem missionSystem,
            ObjectSpawnSystem objectSpawnSystem,
            FieldObjectSystem fieldObjectSystem,
            Action onMissionSuccess,
            Action onMissionFailed)
        {
            timeSystem.OnDayChangedEvent += missionSystem.OnDayChanged;
            timeSystem.OnDayChangedEvent += objectSpawnSystem.OnDayChanged;
            // timeSystem.OnDayChangedEvent += rewardSystem.OnDayChanged;
            missionSystem.OnMissionSuccessEvent += onMissionSuccess;
            missionSystem.OnMissionFailedEvent += onMissionFailed;
            fieldObjectSystem.OnObjectSpawnedEvent += missionSystem.OnObjectSpawned;
        }

        public void Unbind(
            TimeSystem timeSystem,
            MissionSystem missionSystem,
            ObjectSpawnSystem objectSpawnSystem,
            FieldObjectSystem fieldObjectSystem,
            Action onMissionSuccess,
            Action onMissionFailed)
        {
            timeSystem.OnDayChangedEvent -= missionSystem.OnDayChanged;
            timeSystem.OnDayChangedEvent -= objectSpawnSystem.OnDayChanged;
            // timeSystem.OnDayChangedEvent -= rewardSystem.OnDayChanged;
            missionSystem.OnMissionSuccessEvent -= onMissionSuccess;
            missionSystem.OnMissionFailedEvent -= onMissionFailed;
            fieldObjectSystem.OnObjectSpawnedEvent -= missionSystem.OnObjectSpawned;
        }
    }
}
