using Afterlife.Core;
using UnityEngine;

namespace Afterlife.Model
{
    public class Reward
    {
        public void ApplyReward(string rewardId)
        {
            var game = ServiceLocator.Get<GameManager>().Game;

            switch (rewardId)
            {
                case "attack-power":
                    game.Player.AttackPower += 5;
                    Debug.Log("Applied attack power reward.");
                    break;
                case "extra-life":
                    game.Lives += 1;
                    Debug.Log("Applied extra life reward.");
                    break;
                default:
                    Debug.LogWarning($"Unknown reward ID: {rewardId}");
                    break;
            }
        }
    }
}