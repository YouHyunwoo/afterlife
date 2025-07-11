namespace Afterlife.View
{
    public class Wall : Object
    {
        public override void Interact(Model.Player player)
        {
            if (!player.Inventory.ContainsKey("stone")) { return; }
            if (player.Inventory["stone"] <= 0) { return; }

            player.Inventory["stone"]--;
            if (player.Inventory["stone"] <= 0)
            {
                player.Inventory.Remove("stone");
            }

            Value += 1f;
            RefreshValue();
            base.Interact(player);
        }
    }
}