using Afterlife.Core;
using UnityEngine;

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
                var skillSlot = skillSlotListView.SkillSlots[i];
                skillSlot.SkillData = skill.SkillData;
                skillSlot.OnInformationShowed += OnSkillInformationShowed;
                skillSlot.OnInformationHidden += OnSkillInformationHidden;
                Bind(skill, skillSlot);
                skill.SetUp();
            }

            enabled = true;
        }

        void OnSkillInformationShowed(UI.Stage.SkillSlot skillSlot)
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as UI.Stage.Screen;

            var nodeRectTransform = skillSlot.GetComponent<RectTransform>();
            stageScreen.SkillInformationView.GetComponent<RectTransform>().position = nodeRectTransform.position + new Vector3(-25, stageScreen.SkillInformationView.GetComponent<RectTransform>().sizeDelta.y - 50, 0);
            var skillData = skillSlot.SkillData;
            stageScreen.SkillInformationView.Show(skillData.Id, 0);
        }

        void OnSkillInformationHidden(UI.Stage.SkillSlot skillSlot)
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as UI.Stage.Screen;

            stageScreen.SkillInformationView.Hide();
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
                var skillSlot = skillSlotListView.SkillSlots[i];
                skill.TearDown();
                Unbind(skill, skillSlot);
                skillSlot.OnInformationShowed -= OnSkillInformationShowed;
                skillSlot.OnInformationHidden -= OnSkillInformationHidden;
                skillSlot.SkillData = null;
            }

            skillSlotListView.ClearSkillSlots();
            stageScreen.SkillInformationView.Hide();
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