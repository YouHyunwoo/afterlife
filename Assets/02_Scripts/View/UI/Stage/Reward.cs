using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Afterlife.View
{
    public class Reward : UIView
    {
        [SerializeField] Transform rewardSelectionTransform;
        [SerializeField] RewardSelection rewardSelectionPrefab;

        public event Action<int> OnRewardSelected;

        public void SetRewardSelections(Data.Reward[] rewards, int count)
        {
            Clear();
            var rewardSet = new HashSet<int>();
            if (count > rewards.Length)
            {
                count = rewards.Length;
            }
            while (rewardSet.Count < count)
            {
                int randomIndex = UnityEngine.Random.Range(0, rewards.Length);
                rewardSet.Add(randomIndex);
            }
            for (int i = 0; i < rewardSet.Count; i++)
            {
                var rewardIndex = rewardSet.ElementAt(i);
                var reward = rewards[rewardIndex];
                var rewardSelection = Instantiate(rewardSelectionPrefab, rewardSelectionTransform);
                rewardSelection.SetName(reward.Name);
                rewardSelection.SetDescription(reward.Description);
                rewardSelection.OnRewardSelected += () => OnRewardSelected?.Invoke(rewardIndex);
            }
        }

        public void Clear()
        {
            foreach (Transform child in rewardSelectionTransform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}