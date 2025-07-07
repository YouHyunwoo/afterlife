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
        public float AttackDuration;
        public float CriticalRate;
        public float CriticalDamageMultiplier;
        public float RecoveryPower;
        public int MaxEquipmentCount;
        public int RewardSelectionCount;
        public bool IsAutomationEnabled;

        public Light Light;
        public List<string> Upgrades = new();
        public List<Skill> Skills = new();
        public Dictionary<string, int> Inventory = new();
        public HashSet<string> Equipment = new();

        public event Action<float> OnExperienceChanged;

        public void TakeExperience(float experience)
        {
            Experience += experience;
            OnExperienceChanged?.Invoke(Experience);
        }
    }
}