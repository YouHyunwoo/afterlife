using Afterlife.Core;
using UnityEngine;

namespace Afterlife.Model
{
    public class OpenEyes : Skill
    {
        float originalSightRange;
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
            originalSightRange = ServiceLocator.Get<GameManager>().Game.Player.Light.Range;
            ServiceLocator.Get<GameManager>().Game.Player.Light.Range *= SightRangeMultiplier;
            base.OnActivated();
        }

        protected override void OnDeactivated()
        {
            // 원래대로 돌려놓기
            ServiceLocator.Get<GameManager>().Game.Player.Light.Range = originalSightRange;
            Debug.Log($"OpenEyes deactivated: {SightRangeMultiplier}");
        }
    }
}