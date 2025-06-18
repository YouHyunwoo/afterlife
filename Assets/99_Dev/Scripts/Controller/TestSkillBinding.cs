using UnityEngine;

namespace Afterlife.Controller
{
    public class TestSkillBinding : MonoBehaviour
    {
        public Data.Skill[] SkillDataArray;
        public UI.Stage.SkillSlot[] SkillSlotArray;
        public UI.Stage.Skill SkillSlotList;

        Model.Skill[] SkillArray;

        void Start()
        {
            // var controller = GetComponent<Controller>();

            SkillArray = new Model.Skill[SkillDataArray.Length];
            for (int i = 0; i < SkillDataArray.Length; i++)
            {
                var skill = GenerateSkill(SkillDataArray[i]);
                SkillSlotArray[i].SetEnabled(skill != null);
                if (skill == null) { continue; }

                SkillArray[i] = skill;
                BindSkill(SkillArray[i], SkillSlotArray[i]);
                // SkillArray[i].SetUp(controller);
            }
        }

        Model.Skill GenerateSkill(Data.Skill skillData) // Skill Generator
        {
            switch (skillData.Id)
            {
                case "rich-resources":
                    return new Model.RichResources(skillData);
                case "open-eyes":
                    return new Model.OpenEyes(skillData);
                default:
                    Debug.LogError($"Skill with ID {skillData.Id} not found.");
                    return null;
            }
        }

        void BindSkill(Model.Skill skill, UI.Stage.SkillSlot skillSlot)
        {
            skill.OnActivatedEvent += skillSlot.StartFlow;
            skill.OnDeactivatedEvent += skillSlot.StopFlow;
            skill.OnCooldownStartedEvent += skillSlot.ShowCooldown;
            skill.OnCooldownUpdatedEvent += skillSlot.SetCooldownRatio;
            skill.OnCooldownEndedEvent += skillSlot.HideCooldown;
            skillSlot.OnSkillSlotClickedEvent += skill.Use;
            skillSlot.SetIcon(skill.SkillData.IconSprite);
        }
    }
}