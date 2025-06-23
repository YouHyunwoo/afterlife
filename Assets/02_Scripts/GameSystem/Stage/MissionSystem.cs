using System;
using System.Collections.Generic;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class MissionSystem : SystemBase
    {
        HashSet<View.Monster> monsterSet;
        HashSet<View.Village> villageSet;
        bool isTargetDayReached;

        public event Action OnMissionSuccessEvent;
        public event Action OnMissionFailedEvent;

        public override void SetUp()
        {
            monsterSet = new();
            villageSet = new();

            var monsters = GameObject.FindObjectsByType<View.Monster>(FindObjectsSortMode.None);
            foreach (var monster in monsters)
            {
                monsterSet.Add(monster);
                monster.OnDied += OnObjectDied;
            }

            var villages = GameObject.FindObjectsByType<View.Village>(FindObjectsSortMode.None);
            foreach (var village in villages)
            {
                villageSet.Add(village);
                village.OnDied += OnObjectDied;
            }

            isTargetDayReached = false;

            enabled = true;
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
            if (@object is View.Monster monster)
            {
                OnMonsterDied(monster);
            }
            else if (@object is View.Village village)
            {
                OnVillageDied(village);
            }
        }

        void OnMonsterDied(View.Monster monster)
        {
            // TODO: 몬스터가 가진 경험치 획득
            monsterSet.Remove(monster);
            monster.OnDied -= OnObjectDied;

            VerifyMissionSuccess();
        }

        void OnVillageDied(View.Village village)
        {
            villageSet.Remove(village);
            village.OnDied -= OnObjectDied;

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
            if (@object is View.Monster monster)
            {
                OnMonsterSpawned(monster);
            }
        }

        void OnMonsterSpawned(View.Monster monster)
        {
            monsterSet.Add(monster);
            monster.OnDied += OnObjectDied;
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
    }
}