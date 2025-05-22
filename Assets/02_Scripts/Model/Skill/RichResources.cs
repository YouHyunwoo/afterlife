using UnityEngine;

namespace Afterlife.Model
{
    public class RichResources : Skill
    {
        public float SpawningAmountMultiplier;
        public float SpawningProbabilityMultiplier;

        public RichResources(Data.Skill skillData) : base(skillData)
        {
            foreach (var parameter in skillData.SkillParameterGroups)
            {
                if (parameter.Key == "spawning-amount-multiplier")
                {
                    SpawningAmountMultiplier = float.Parse(parameter.Value);
                }
                else if (parameter.Key == "spawning-probability-multiplier")
                {
                    SpawningProbabilityMultiplier = float.Parse(parameter.Value);
                }
            }
        }

        public override void SetUp(Controller.Controller controller)
        {
            base.SetUp(controller);
            Use();
        }

        protected override void OnActivated()
        {
            controller.Game.Stage.MaxEnvironmentObjectCount = Mathf.FloorToInt(controller.Game.Stage.MaxEnvironmentObjectCount * SpawningAmountMultiplier);
            controller.Game.Stage.EnvironmentObjectGenerationProbability *= SpawningProbabilityMultiplier;
            Debug.Log($"RichResources activated: {SpawningAmountMultiplier} {SpawningProbabilityMultiplier}");
        }

        protected override void OnDeactivated()
        {
            controller.Game.Stage.MaxEnvironmentObjectCount = Mathf.FloorToInt(controller.Game.Stage.MaxEnvironmentObjectCount / SpawningAmountMultiplier); ;
            controller.Game.Stage.EnvironmentObjectGenerationProbability /= SpawningProbabilityMultiplier;
        }
    }
}