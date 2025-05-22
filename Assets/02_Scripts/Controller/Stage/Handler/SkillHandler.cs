using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Controller
{
    public class SkillHandler : Handler
    {
        readonly Model.Stage stage;

        public List<Model.Skill> Skills;

        public SkillHandler(Controller controller) : base(controller)
        {
            stage = controller.Game.Stage;

            Skills = new();
        }

        public override void SetUp()
        {
            var skills = controller.Game.Player.Skills;

            var skillSlotListView = controller.StageView.SkillSlotListView;
            // skillSlotListView.ClearSkillSlots(); // 여기서 Clear하면 에러남
            for (int i = 0; i < skills.Count; i++)
            {
                skillSlotListView.GenerateSkillSlot();
            }

            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                var skillSlot = skillSlotListView.transform.GetChild(i).GetComponent<View.SkillSlot>();
                Bind(skill, skillSlot);
                skill.SetUp(controller);
            }
        }

        public override void TearDown()
        {
            var skills = controller.Game.Player.Skills;
            var skillSlotListView = controller.StageView.SkillSlotListView;
            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                var skillSlot = skillSlotListView.transform.GetChild(i).GetComponent<View.SkillSlot>();
                skill.TearDown();
                Unbind(skill, skillSlot);
            }
            skillSlotListView.ClearSkillSlots();
        }

        void Bind(Model.Skill skill, View.SkillSlot skillSlot)
        {
            skill.OnActivatedEvent += skillSlot.StartFlow;
            skill.OnDeactivatedEvent += skillSlot.StopFlow;
            skill.OnCooldownStartedEvent += skillSlot.ShowCooldown;
            skill.OnCooldownUpdatedEvent += skillSlot.SetCooldownRatio;
            skill.OnCooldownEndedEvent += skillSlot.HideCooldown;
            skillSlot.OnSkillSlotClickedEvent += skill.Use;
            skillSlot.SetIcon(skill.SkillData.IconSprite);
        }

        void Unbind(Model.Skill skill, View.SkillSlot skillSlot)
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