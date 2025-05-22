using UnityEngine;

namespace Afterlife.Model
{
    public class OpenEyes : Skill
    {
        public float SightRangeMultiplier;

        public OpenEyes(Data.Skill skillData) : base(skillData)
        {
            foreach (var parameter in skillData.SkillParameterGroups)
            {
                if (parameter.Key == "sight-range-multiplier")
                {
                    SightRangeMultiplier = float.Parse(parameter.Value);
                }
            }
        }

        protected override void OnActivated()
        {
            Debug.Log($"OpenEyes activated: {SightRangeMultiplier}");
            base.OnActivated();
        }

        protected override void OnDeactivated()
        {
            // 원래대로 돌려놓기
            Debug.Log($"OpenEyes deactivated: {SightRangeMultiplier}");
        }
    }
}