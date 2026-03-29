using System;
using System.Collections.Generic;

namespace Afterlife.Dev.Field
{
    public class GameResultSystem : Moonstone.Ore.Local.System
    {
        readonly HashSet<ObjectVisible> houses = new();
        ObjectVisible boss;
        bool isResolved;

        public event Action OnGameClearEvent;
        public event Action OnGameOverEvent;

        public void RegisterHouse(ObjectVisible house)
        {
            if (houses.Add(house))
                house.OnDied += OnHouseDied;
        }

        public void UnregisterHouse(ObjectVisible house)
        {
            if (houses.Remove(house))
                house.OnDied -= OnHouseDied;
        }

        public void RegisterBoss(ObjectVisible newBoss)
        {
            if (boss != null)
                boss.OnDied -= OnBossDied;

            boss = newBoss;
            boss.OnDied += OnBossDied;
        }

        public void UnregisterBoss(ObjectVisible oldBoss)
        {
            if (boss != oldBoss) return;
            boss.OnDied -= OnBossDied;
            boss = null;
        }

        public void Reset()
        {
            foreach (var h in houses) h.OnDied -= OnHouseDied;
            houses.Clear();

            if (boss != null)
            {
                boss.OnDied -= OnBossDied;
                boss = null;
            }

            isResolved = false;
        }

        void OnHouseDied(ObjectVisible attacker, ObjectVisible house, object _)
        {
            UnregisterHouse(house);
            CheckGameOver();
        }

        void OnBossDied(ObjectVisible attacker, ObjectVisible _, object __)
        {
            if (isResolved) return;
            isResolved = true;
            OnGameClearEvent?.Invoke();
        }

        void CheckGameOver()
        {
            if (isResolved) return;
            if (houses.Count > 0) return;

            isResolved = true;
            OnGameOverEvent?.Invoke();
        }
    }
}
