using UnityEngine;

namespace Afterlife.View
{
    public class Wall : Object
    {
        [Header("Interaction")]
        [SerializeField] string requiredItemName;
        [SerializeField] int requiredItemAmount;

        public override void Interact(Model.Player player)
        {
            var inventory = player.Inventory;

            if (!inventory.HasItem(requiredItemName, requiredItemAmount)) { return; }
            inventory.RemoveItem(requiredItemName, requiredItemAmount, out var _);

            Value += 1f;
            RefreshValue();
            base.Interact(player);
        }
    }
}