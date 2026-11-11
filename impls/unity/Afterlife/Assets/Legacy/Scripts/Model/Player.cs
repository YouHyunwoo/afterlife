using System;
using System.Collections.Generic;

namespace Afterlife.Model
{
    public class Player
    {
        public float Experience;
        public int Level;
        public float CurrentLevelExperience;
        public float RequiredLevelExperience;
        public float LevelExperienceGrowth;
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
        public int AvailableArtifactSelectionCount;

        public Light Light;
        public List<string> Upgrades = new();
        public List<string> SelectedArtifactIds = new();
        public HashSet<string> UnlockedRecipeIds = new();
        public List<Skill> Skills = new();
        public Inventory Inventory = new();
        public HashSet<string> Equipment = new();

        public event Action<float> OnExperienceChanged;
        public event Action<int> OnLevelChanged;
        public event Action<int> OnArtifactSelectionCountChanged;

        public void TakeExperience(float experience)
        {
            if (experience <= 0f) { return; }

            Experience += experience;
            CurrentLevelExperience += experience;
            ProcessLevelUp();
            OnExperienceChanged?.Invoke(Experience);
        }

        public bool SpendExperience(float experience)
        {
            if (experience <= 0f) { return true; }
            if (Experience < experience) { return false; }

            Experience -= experience;
            OnExperienceChanged?.Invoke(Experience);
            return true;
        }

        public bool ConsumeArtifactSelection()
        {
            if (AvailableArtifactSelectionCount <= 0) { return false; }

            AvailableArtifactSelectionCount--;
            OnArtifactSelectionCountChanged?.Invoke(AvailableArtifactSelectionCount);
            return true;
        }

        void ProcessLevelUp()
        {
            if (RequiredLevelExperience <= 0f) { return; }

            while (CurrentLevelExperience >= RequiredLevelExperience)
            {
                CurrentLevelExperience -= RequiredLevelExperience;
                Level++;
                AvailableArtifactSelectionCount++;
                RequiredLevelExperience *= LevelExperienceGrowth > 0f ? LevelExperienceGrowth : 1.2f;
                OnLevelChanged?.Invoke(Level);
                OnArtifactSelectionCountChanged?.Invoke(AvailableArtifactSelectionCount);
            }
        }
    }
}
