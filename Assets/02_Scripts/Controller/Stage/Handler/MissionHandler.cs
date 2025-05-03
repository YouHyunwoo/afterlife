using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Afterlife.Controller
{
    public class MissionHandler : Handler
    {
        readonly TimeHandler timeHandler;

        HashSet<View.Monster> monsterSet;
        bool isTargetDayReached;
        HashSet<View.Village> villageSet;

        public event Action OnMissionSuccessEvent;
        public event Action OnMissionFailedEvent;

        public MissionHandler(Controller controller) : base(controller)
        {
            timeHandler = controller.StageSceneController.StageController.TimeHandler;
        }

        public override void SetUp()
        {
            isTargetDayReached = false;
            timeHandler.OnDayChangedEvent += OnDayChanged;

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
        }

        public override void TearDown()
        {
            timeHandler.OnDayChangedEvent -= OnDayChanged;
            monsterSet.Clear();
            monsterSet = null;
            villageSet.Clear();
            villageSet = null;
        }

        public void OnMonsterSpawned(View.Monster monster)
        {
            monsterSet.Add(monster);
            monster.OnDied += OnObjectDied;
        }

        void OnObjectDied(View.Object @object)
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

        void OnDayChanged(int day)
        {
            if (isTargetDayReached) { return; }
            if (day >= controller.Game.Stage.Data.SpawnIntervalPerDay.Length)
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
    }
}