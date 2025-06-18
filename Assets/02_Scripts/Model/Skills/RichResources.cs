using Afterlife.Core;
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

        public override void SetUp()
        {
            Use();
        }

        protected override void OnActivated()
        {
            ServiceLocator.Get<GameManager>().Game.Stage.MaxEnvironmentObjectCount = Mathf.FloorToInt(ServiceLocator.Get<GameManager>().Game.Stage.MaxEnvironmentObjectCount * SpawningAmountMultiplier);
            ServiceLocator.Get<GameManager>().Game.Stage.EnvironmentObjectGenerationProbability *= SpawningProbabilityMultiplier;
            Debug.Log($"RichResources activated: {SpawningAmountMultiplier} {SpawningProbabilityMultiplier}");
        }

        protected override void OnDeactivated()
        {
            ServiceLocator.Get<GameManager>().Game.Stage.MaxEnvironmentObjectCount = Mathf.FloorToInt(ServiceLocator.Get<GameManager>().Game.Stage.MaxEnvironmentObjectCount / SpawningAmountMultiplier);
            ServiceLocator.Get<GameManager>().Game.Stage.EnvironmentObjectGenerationProbability /= SpawningProbabilityMultiplier;
        }
    }
}