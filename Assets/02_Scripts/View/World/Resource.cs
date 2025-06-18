using UnityEngine;

namespace Afterlife.View
{
    public class Resource : Object
    {
        public string Type;
        public int Amount;

        public override void Interact(Model.Player player)
        {
            void OnDiedEvent(Object attacker, Object @object)
            {
                var inventory = player.Inventory;
                if (!inventory.ContainsKey(Type)) { inventory[Type] = 0; }
                player.Inventory[Type] += Amount;
                Debug.Log($"Collected {Amount} of {Type}. Total: {player.Inventory[Type]}");
            }

            OnDied += OnDiedEvent;
            TakeDamage(player.AttackPower, null);
            base.Interact(player);
            OnDied -= OnDiedEvent;
        }
    }
}