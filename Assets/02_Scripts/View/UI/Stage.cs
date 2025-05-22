namespace Afterlife.View
{
    public class Stage : UIView
    {
        public Experience ExperienceView;
        public SkillSlotList SkillSlotListView;

        public void SetExperience(float experience) => ExperienceView.SetExperience(experience);
    }
}