using System;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class Reward : View
    {
        public RewardListItem[] RewardListItems;

        public event Action<RewardListItem> OnRewardListItemClickedEvent;

        void Awake()
        {
            foreach (var item in RewardListItems)
            {
                item.OnRewardListItemClickedEvent += OnRewardListItemClicked;
            }
        }

        void OnRewardListItemClicked(RewardListItem item) => OnRewardListItemClickedEvent?.Invoke(item);

        public void SetRewardList(Data.Reward[] rewardCandidates)
        {
            for (int i = 0; i < RewardListItems.Length; i++)
            {
                if (i >= rewardCandidates.Length)
                {
                    RewardListItems[i].gameObject.SetActive(false);
                    continue;
                }
                var reward = rewardCandidates[i];
                var item = RewardListItems[i];
                var rewardName = Localization.Get($"reward.{reward.Id}.name");
                var rewardDescription = Localization.Get($"reward.{reward.Id}.description");
                item.RewardId = reward.Id;
                item.SetName(rewardName);
                item.SetIcon(reward.Icon);
                item.SetDescription(rewardDescription);
                item.gameObject.SetActive(true);
            }
        }
    }
}