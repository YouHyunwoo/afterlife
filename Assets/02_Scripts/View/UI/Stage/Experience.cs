using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class Experience : UIView
    {
        [SerializeField] Image experienceBarImage;

        public void SetExperienceRatio(float ratio) => experienceBarImage.fillAmount = ratio;
    }
}