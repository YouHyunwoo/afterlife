using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Stage : UIView
    {
        [SerializeField] Experience experienceView;

        public void SetExperienceRatio(float ratio) => experienceView.SetExperienceRatio(ratio);
    }
}