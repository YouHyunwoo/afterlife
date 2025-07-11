using System;
using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "DayData", menuName = "Afterlife/Data/Day")]
    public class Day : ScriptableObject
    {
        [Header("Difficulty")]
        public float ValueMultiplier = 1f;

        [Header("Always")]
        public ObjectSpawn AlwaysObjectSpawn;

        [Header("Day")]
        public float DayDuration;
        public ObjectSpawn DayObjectSpawn;

        [Header("Night")]
        public float NightDuration;
        public ObjectSpawn NightObjectSpawn;

        [Header("Resources")]
        public int MaxResourceCount;

        // Events
        // Rewards
    }

    [Serializable]
    public class ObjectSpawn
    {
        public float SpawnInterval;
        public ObjectSpawnPrefabWeightGroup[] PrefabWeightGroups;

        public GameObject Sample()
        {
            if (PrefabWeightGroups == null || PrefabWeightGroups.Length == 0) { return null; }

            float totalWeight = 0f;
            foreach (var group in PrefabWeightGroups)
            {
                totalWeight += group.Weight;
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            foreach (var group in PrefabWeightGroups)
            {
                cumulativeWeight += group.Weight;
                if (randomValue <= cumulativeWeight)
                {
                    return group.Prefab;
                }
            }

            return null; // Fallback if no prefab is selected
        }
    }

    [Serializable]
    public class ObjectSpawnPrefabWeightGroup
    {
        public GameObject Prefab;
        public float Weight;
    }
}