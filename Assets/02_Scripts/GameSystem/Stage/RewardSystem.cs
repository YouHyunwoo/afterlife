using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class RewardSystem : SystemBase
    {
        int cumulativeCount;
        UI.Stage.Reward rewardView;

        public override void SetUp()
        {
            var stageScreen = ServiceLocator.Get<UIManager>().InGameScreen as UI.Stage.Screen;
            rewardView = stageScreen.RewardView;
            rewardView.OnRewardListItemClickedEvent += OnRewardListItemClicked;

            cumulativeCount = 0;
        }

        void OnRewardListItemClicked(UI.Stage.RewardListItem item)
        {
            // Handle the reward item click event.
            // For example, you might want to grant the reward to the player.
            var rewardId = item.RewardId;
            GrantReward(rewardId);
            cumulativeCount--;

            if (cumulativeCount > 0)
            {
                var availableRewards = GetAvailableRewards();
                rewardView.SetRewardList(availableRewards);
            }
            else
            {
                rewardView.Hide();
            }
        }

        public override void TearDown()
        {
            rewardView.Hide();
            rewardView.OnRewardListItemClickedEvent -= OnRewardListItemClicked;
            rewardView = null;
        }

        public void OnDayChanged(int day)
        {
            if (rewardView == null) { return; }
            // This method can be used to handle any logic that needs to occur when the day changes.
            // For example, you might want to update the available rewards based on the current day.
            var availableRewards = GetAvailableRewards();
            rewardView.SetRewardList(availableRewards);
            rewardView.Show();

            cumulativeCount++;
        }

        Data.Reward[] GetAvailableRewards()
        {
            // This method should return the available rewards for the given day.
            // For now, we will return an empty array as a placeholder.
            // You can replace this with actual logic to fetch rewards based on the day.
            var rewards = ServiceLocator.Get<DataManager>().RewardDataArray;
            var availableRewards = new System.Collections.Generic.List<Data.Reward>();
            foreach (var reward in rewards)
            {
                if (reward.Type == Data.RewardType.Day)
                {
                    availableRewards.Add(reward);
                }
            }
            // Sample 3 rewards
            if (availableRewards.Count > 3)
            {
                availableRewards = availableRewards.GetRange(0, 3);
            }
            else if (availableRewards.Count == 0)
            {
                Debug.LogWarning("No rewards available for the current day.");
            }

            return availableRewards.ToArray();
        }

        public void GrantReward(string rewardId)
        {
            ServiceLocator.Get<GameManager>().Game.Reward.ApplyReward(rewardId);
        }
    }
}