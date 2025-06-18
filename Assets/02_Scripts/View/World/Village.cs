namespace Afterlife.View
{
    public class Village : Object
    {
        public override void Interact(Model.Player player)
        {
            Health += player.RecoveryPower;
            UpdateValue();
            base.Interact(player);
        }
    }
}