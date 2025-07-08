using Afterlife.Core;

namespace Afterlife.GameSystem.Stage
{
    public class EquipmentSystem : SystemBase
    {
        public bool TryToggleEquipment(Data.Item itemData, out bool isEquipped)
        {
            var gameManager = ServiceLocator.Get<GameManager>();
            var player = gameManager.Game.Player;

            if (player.Equipment.Contains(itemData.Id))
            {
                player.Equipment.Remove(itemData.Id);
                ApplyEffect(itemData, false);
                isEquipped = false;
            }
            else
            {
                if (player.Equipment.Count >= player.MaxEquipmentCount)
                {
                    isEquipped = false;
                    return false;
                }

                player.Equipment.Add(itemData.Id);
                ApplyEffect(itemData, true);
                isEquipped = true;
            }

            return true;
        }

        void ApplyEffect(Data.Item itemData, bool apply)
        {
            var gameManager = ServiceLocator.Get<GameManager>();
            var player = gameManager.Game.Player;

            foreach (var effect in itemData.Effects)
            {
                switch (effect.EffectType)
                {
                    case "attack-power":
                        player.AttackPower += apply ? effect.Values[0] : -effect.Values[0];
                        break;
                    case "attack-speed":
                        player.AttackSpeed += apply ? effect.Values[0] : -effect.Values[0];
                        break;
                    case "attack-range":
                        player.AttackRange += apply ? effect.Values[0] : -effect.Values[0];
                        break;
                    case "add-critical-rate":
                        player.CriticalRate += apply ? effect.Values[0] : -effect.Values[0];
                        break;
                    case "add-critical-damage-multiplier":
                        player.CriticalDamageMultiplier += apply ? effect.Values[0] : -effect.Values[0];
                        break;
                    case "add-sight-range":
                        player.Light.Range += apply ? effect.Values[0] : -effect.Values[0];
                        gameManager.Game.Stage.Map.Fog.Update();
                        break;
                }
            }
        }
    }
}