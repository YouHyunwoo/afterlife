using Afterlife.Core;

namespace Afterlife.GameSystem.Stage
{
    public class SkillSystem : SystemBase
    {
        public override void SetUp()
        {
            var skills = ServiceLocator.Get<GameManager>().Game.Player.Skills;

            var stageScreen = ServiceLocator.Get<UIManager>().InGameScreen as UI.Stage.Screen;
            var skillSlotListView = stageScreen.SkillView;
            // skillSlotListView.ClearSkillSlots(); // 여기서 Clear하면 에러남

            for (int i = 0; i < skills.Count; i++)
            {
                skillSlotListView.GenerateSkillSlot();
            }

            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                var skillSlot = skillSlotListView.transform.GetChild(i).GetComponent<UI.Stage.SkillSlot>();
                Bind(skill, skillSlot);
                skill.SetUp();
            }

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            var skills = ServiceLocator.Get<GameManager>().Game.Player.Skills;
            var stageScreen = ServiceLocator.Get<UIManager>().InGameScreen as UI.Stage.Screen;
            var skillSlotListView = stageScreen.SkillView;

            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                var skillSlot = skillSlotListView.transform.GetChild(i).GetComponent<UI.Stage.SkillSlot>();
                skill.TearDown();
                Unbind(skill, skillSlot);
            }

            skillSlotListView.ClearSkillSlots();
        }

        void Bind(Model.Skill skill, UI.Stage.SkillSlot skillSlot)
        {
            skill.OnActivatedEvent += skillSlot.StartFlow;
            skill.OnDeactivatedEvent += skillSlot.StopFlow;
            skill.OnCooldownStartedEvent += skillSlot.ShowCooldown;
            skill.OnCooldownUpdatedEvent += skillSlot.SetCooldownRatio;
            skill.OnCooldownEndedEvent += skillSlot.HideCooldown;
            skillSlot.OnSkillSlotClickedEvent += skill.Use;
            skillSlot.SetIcon(skill.SkillData.IconSprite);
        }

        void Unbind(Model.Skill skill, UI.Stage.SkillSlot skillSlot)
        {
            skill.OnActivatedEvent -= skillSlot.StartFlow;
            skill.OnDeactivatedEvent -= skillSlot.StopFlow;
            skill.OnCooldownStartedEvent -= skillSlot.ShowCooldown;
            skill.OnCooldownUpdatedEvent -= skillSlot.SetCooldownRatio;
            skill.OnCooldownEndedEvent -= skillSlot.HideCooldown;
            skillSlot.OnSkillSlotClickedEvent -= skill.Use;
        }
    }
}