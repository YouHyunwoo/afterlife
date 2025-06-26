using Afterlife.Core;
using UnityEngine;

namespace Afterlife.Model
{
    public class Upgrade
    {
        public void ApplyUpgrade(string upgradeId)
        {
            var game = ServiceLocator.Get<GameManager>().Game;

            switch (upgradeId)
            {
                case "attack-power-1":
                    game.Player.AttackPower += 1;
                    Debug.Log("Applied attack power upgrade.");
                    break;
                case "attack-power-2":
                    game.Player.AttackPower += 2;
                    Debug.Log("Applied attack power upgrade level 2.");
                    break;
                case "attack-power-3":
                    game.Player.AttackPower += 5;
                    Debug.Log("Applied attack power upgrade level 3.");
                    break;
                case "attack-power-4":
                    game.Player.AttackPower += 10;
                    Debug.Log("Applied attack power upgrade level 4.");
                    break;
                case "attack-power-5":
                    game.Player.AttackPower += 20;
                    Debug.Log("Applied attack power upgrade level 5.");
                    break;
                case "auto-attack":
                    Debug.Log("Applied auto attack upgrade.");
                    break;
                case "attack-speed-1":
                    game.Player.AttackSpeed += 0.1f;
                    Debug.Log("Applied attack speed upgrade.");
                    break;
                case "attack-speed-2":
                    game.Player.AttackSpeed += 0.2f;
                    Debug.Log("Applied attack speed upgrade level 2.");
                    break;
                case "attack-speed-3":
                    game.Player.AttackSpeed += 0.3f;
                    Debug.Log("Applied attack speed upgrade level 3.");
                    break;
                case "attack-range-1":
                    game.Player.AttackRange += 1f;
                    Debug.Log("Applied attack range upgrade.");
                    break;
                case "attack-range-2":
                    game.Player.AttackRange += 2f;
                    Debug.Log("Applied attack range upgrade Level 2.");
                    break;
                case "attack-range-3":
                    game.Player.AttackRange += 3f;
                    Debug.Log("Applied attack range upgrade Level 3.");
                    break;
                case "sight-range-1":
                    game.Player.Light.Range += 1f;
                    break;
                case "sight-range-2":
                    game.Player.Light.Range += 1f;
                    break;
                case "sight-range-3":
                    game.Player.Light.Range += 1f;
                    break;
                case "campfire":
                    {
                        break;
                    }
                case "rich-resources":
                    {
                        var skillData = ServiceLocator.Get<DataManager>().SkillDataDictionary[upgradeId];
                        game.Player.Skills.Add(new RichResources(skillData));
                        break;
                    }
                case "open-eyes":
                    {
                        var skillData = ServiceLocator.Get<DataManager>().SkillDataDictionary[upgradeId];
                        game.Player.Skills.Add(new OpenEyes(skillData));
                        break;
                    }
                default:
                    throw new System.ArgumentException($"Unknown upgrade ID: {upgradeId}");
            }
        }
    }
}