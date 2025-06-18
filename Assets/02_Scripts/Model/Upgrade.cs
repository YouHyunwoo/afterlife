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
                case "attack-range-1":
                    Debug.Log("Applied attack range upgrade.");
                    break;
                default:
                    throw new System.ArgumentException($"Unknown upgrade ID: {upgradeId}");
            }
        }
    }
}