using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Stage : UIView
    {
        public Energy energyView;
        public Experience experienceView;
        public Reward rewardView;

        public void SetRewardSelections(Data.Reward[] rewards, int count) => rewardView.SetRewardSelections(rewards, count);
        public void SetEnergy(float energy) => energyView.SetEnergy(energy);
        public void SetExperienceRatio(float ratio) => experienceView.SetExperienceRatio(ratio);
    }
}