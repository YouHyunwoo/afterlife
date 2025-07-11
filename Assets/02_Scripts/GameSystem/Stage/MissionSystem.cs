using System;
using System.Collections.Generic;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class MissionSystem : SystemBase
    {
        HashSet<View.Object> monsterSet;
        HashSet<View.Village> villageSet;
        bool isTargetDayReached;

        public event Action OnMissionSuccessEvent;
        public event Action OnMissionFailedEvent;

        public override void SetUp()
        {
            monsterSet = new();
            villageSet = new();

            isTargetDayReached = false;

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            isTargetDayReached = false;

            monsterSet.Clear();
            monsterSet = null;
            villageSet.Clear();
            villageSet = null;
        }

        public void OnDayChanged(int day)
        {
            if (isTargetDayReached) { return; }
            var stageManager = ServiceLocator.Get<StageManager>();
            if (day >= stageManager.Stage.Data.DayDataArray.Length)
            {
                isTargetDayReached = true;
                VerifyMissionSuccess();
            }
        }

        void VerifyMissionSuccess()
        {
            if (!isTargetDayReached) { return; }
            if (monsterSet.Count > 0) { return; }

            Debug.Log("All monsters are dead. Stage cleared!");
            OnMissionSuccessEvent?.Invoke();
        }

        void OnObjectDied(View.Object attacker, View.Object @object)
        {
            if (!enabled) { return; }

            if (@object is View.Monster monster)
            {
                OnMonsterDied(monster);
            }
            else if (@object is View.Portal portal)
            {
                OnPortalDied(portal);
            }
            else if (@object is View.Village village)
            {
                OnVillageDied(village);
            }
        }

        void OnMonsterDied(View.Monster monster)
        {
            monsterSet.Remove(monster);
            monster.OnDiedEvent -= OnObjectDied;

            VerifyMissionSuccess();
        }

        void OnPortalDied(View.Portal portal)
        {
            monsterSet.Remove(portal);
            portal.OnDiedEvent -= OnObjectDied;

            VerifyMissionSuccess();
        }

        void OnVillageDied(View.Village village)
        {
            villageSet.Remove(village);
            village.OnDiedEvent -= OnObjectDied;

            VerifyMissionFailure();
        }

        void VerifyMissionFailure()
        {
            // 마을이 다 죽으면 미션 실패
            if (villageSet.Count > 0) { return; }

            Debug.Log("All villages are dead. Stage failed!");
            OnMissionFailedEvent?.Invoke();
        }

        public void OnObjectSpawned(View.Object @object)
        {
            if (@object is View.Monster ||
                @object is View.Portal)
            {
                monsterSet.Add(@object);
                @object.OnDiedEvent += OnObjectDied;
            }
            else if (@object is View.Village village)
            {
                villageSet.Add(village);
                village.OnDiedEvent += OnObjectDied;
            }
        }
    }
}