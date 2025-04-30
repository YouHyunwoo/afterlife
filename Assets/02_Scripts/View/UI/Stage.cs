using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Stage : UIView
    {
        [SerializeField] Energy energyView;
        [SerializeField] Experience experienceView;

        public void SetEnergy(float energy) => energyView.SetEnergy(energy);
        public void SetExperienceRatio(float ratio) => experienceView.SetExperienceRatio(ratio);
    }
}