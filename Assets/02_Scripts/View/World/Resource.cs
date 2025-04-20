using UnityEngine;

namespace Afterlife.View
{
    public class Resource : Object
    {
        public string Type;
        public int Amount;

        public override void Interact(Model.Player player)
        {
            base.Interact(player);
        }

        public override void Died(Model.Player player)
        {
            var inventory = player.Inventory;
            if (!inventory.ContainsKey(Type)) { inventory.Add(Type, 0); }
            player.Inventory[Type] += Amount;
            Debug.Log($"Collected {Amount} of {Type}. Total: {player.Inventory[Type]}");

            base.Died(player);
        }
    }
}