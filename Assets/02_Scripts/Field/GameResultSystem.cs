using System;
using System.Collections.Generic;

namespace Afterlife.Dev.Field
{
    public class GameResultSystem : Moonstone.Ore.Local.System
    {
        readonly HashSet<Object> houses = new();
        Object boss;
        bool isResolved;

        public event Action OnGameClearEvent;
        public event Action OnGameOverEvent;

        public void RegisterHouse(Object house)
        {
            if (houses.Add(house))
                house.OnDied += OnHouseDied;
        }

        public void UnregisterHouse(Object house)
        {
            if (houses.Remove(house))
                house.OnDied -= OnHouseDied;
        }

        public void RegisterBoss(Object newBoss)
        {
            if (boss != null)
                boss.OnDied -= OnBossDied;

            boss = newBoss;
            boss.OnDied += OnBossDied;
        }

        public void UnregisterBoss(Object oldBoss)
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

        void OnHouseDied(Object attacker, Object house, object _)
        {
            UnregisterHouse(house);
            CheckGameOver();
        }

        void OnBossDied(Object attacker, Object _, object __)
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
