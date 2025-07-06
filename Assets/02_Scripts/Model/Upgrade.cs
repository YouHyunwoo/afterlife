using Afterlife.Core;
using UnityEngine;

namespace Afterlife.Model
{
    public class Upgrade
    {
        public void ApplyUpgrade(string upgradeId)
        {
            var upgradeData = ServiceLocator.Get<DataManager>().UpgradeDataDictionary[upgradeId];
            if (upgradeData == null)
            {
                Debug.LogError($"Upgrade data not found for ID: {upgradeId}");
                return;
            }

            foreach (var parameter in upgradeData.Parameters)
            {
                ApplyUpgradeParameter(parameter);
            }
        }

        void ApplyUpgradeParameter(Data.UpgradeParameter parameter)
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
                    break;
                case "get-skill":
                    var skillId = parameterValues[0];
                    var skillData = ServiceLocator.Get<DataManager>().SkillDataDictionary[skillId];
                    if (skillData != null)
                    {
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
                    else
                    {
                        Debug.LogWarning($"Skill data not found for ID: {skillId}");
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown upgrade parameter: {parameterName}");
                    break;
            }
        }
    }
}