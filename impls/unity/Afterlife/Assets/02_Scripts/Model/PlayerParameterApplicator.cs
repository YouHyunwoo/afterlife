using Afterlife.Core;
using Afterlife.Domain.Progression.Effect.Data;
using UnityEngine;

namespace Afterlife.Model
{
    public static class PlayerParameterApplicator
    {
        public static void Apply(PlayerEffectParameter parameter)
        {
            var parameterName = parameter.Name;
            var parameterValues = parameter.Values;

            var game = ServiceLocator.Get<GameManager>().Game;

            switch (parameterName)
            {
                case "add-attack-power":
                    game.Player.AttackPower += int.Parse(parameterValues[0]);
                    break;
                case "add-critical-rate":
                    game.Player.CriticalRate += float.Parse(parameterValues[0]);
                    break;
                case "add-critical-damage-multiplier":
                    game.Player.CriticalDamageMultiplier += float.Parse(parameterValues[0]);
                    break;
                case "attack-speed":
                    game.Player.AttackSpeed = float.Parse(parameterValues[0]);
                    break;
                case "attack-range":
                    game.Player.AttackRange = float.Parse(parameterValues[0]);
                    break;
                case "attack-duration":
                    game.Player.AttackDuration = float.Parse(parameterValues[0]);
                    break;
                case "add-recovery-power":
                    game.Player.RecoveryPower += float.Parse(parameterValues[0]);
                    break;
                case "add-sight-range":
                    game.Player.Light.Range += float.Parse(parameterValues[0]);
                    break;
                case "enable-automation":
                    game.Player.IsAutomationEnabled = true;
                    break;
                case "get-skill":
                    ApplySkill(parameterValues[0]);
                    break;
                default:
                    Debug.LogWarning($"Unknown player parameter: {parameterName}");
                    break;
            }
        }

        static void ApplySkill(string skillId)
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            var skillData = ServiceLocator.Get<DataManager>().SkillDataDictionary[skillId];
            if (skillData == null)
            {
                Debug.LogWarning($"Skill data not found for ID: {skillId}");
                return;
            }

            switch (skillId)
            {
                case "rich-resources":
                    game.Player.Skills.Add(new RichResources(skillData));
                    break;
                case "open-eyes":
                    game.Player.Skills.Add(new OpenEyes(skillData));
                    break;
                default:
                    Debug.LogWarning($"Unknown skill ID: {skillId}");
                    break;
            }
        }
    }
}
