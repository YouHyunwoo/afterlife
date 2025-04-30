using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Model
{
    public class Player
    {
        public int Level;
        public float Energy;
        public float Experience;
        public float MaxExperience;
        public float AttackPower;
        public float AttackSpeed;
        public float AttackRange;
        public float AttackCount;
        public float CriticalRate;
        public float CriticalDamageMultiplier;

        public float RecoveryPower = 1f;

        public Dictionary<string, int> Inventory = new();
        public Light Light;

        public event Action<float> OnEnergyChanged;
        public event Action<float> OnExperienceChanged;

        public void TakeExperience(float experience)
        {
            Experience += experience;
            if (Experience >= MaxExperience)
            {
                Experience -= MaxExperience;
                LevelUp();
            }
            OnExperienceChanged?.Invoke(Experience / MaxExperience);

            Energy += experience;
            OnEnergyChanged?.Invoke(Energy);
        }

        void LevelUp()
        {
            Level++;
            MaxExperience = Mathf.FloorToInt(MaxExperience * 1.2f);
        }
    }
}