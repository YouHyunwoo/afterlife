using Afterlife.Core;

namespace Afterlife.View
{
    public class Village : Object
    {
        public override void Interact(Model.Player player)
        {
            ServiceLocator.Get<EffectManager>().PlayGFX("Heal", transform.position);
            ServiceLocator.Get<AudioManager>().PlaySFX("heal");
            Health += player.RecoveryPower;
            UpdateValue();
            base.Interact(player);
        }
    }
}