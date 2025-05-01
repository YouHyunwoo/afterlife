using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Stage : UIView
    {
        public Experience experienceView;

        public void SetExperience(float experience) => experienceView.SetExperience(experience);
    }
}