using UnityEngine;

namespace Afterlife.View
{
    public class Stage : UIView
    {
        public GameObject NightCoverView;
        public Experience ExperienceView;
        public Days StageDayView;
        public StageMenu StageMenuView;
        public SkillSlotList SkillSlotListView;

        public void SetExperience(float experience) => ExperienceView.SetExperience(experience);
    }
}