using System;
using System.Collections.Generic;

namespace Afterlife.Model
{
    public class Player
    {
        public float Experience;
        public float AttackPower;
        public float AttackSpeed;
        public float AttackRange;
        public float AttackCount;
        public float CriticalRate;
        public float CriticalDamageMultiplier;
        public int RewardSelectionCount;

        public float RecoveryPower = 1f;

        public List<string> Upgrades = new();
        public List<Skill> Skills = new();
        public Dictionary<string, int> Inventory = new();
        public Light Light;

        public event Action<float> OnExperienceChanged;

        public void TakeExperience(float experience)
        {
            Experience += experience;
            OnExperienceChanged?.Invoke(Experience);
        }
    }
}