using Afterlife.Core;

namespace Afterlife.View
{
    public class Village : Object
    {
        public override void Interact(Model.Player player)
        {
            ServiceLocator.Get<EffectManager>().PlayGFX("Heal", transform.position);
            ServiceLocator.Get<AudioManager>().PlaySFX("heal");
            var isCriticalHit = UnityEngine.Random.value < player.CriticalRate;
            if (isCriticalHit) { ServiceLocator.Get<AudioManager>().PlaySFX("critical"); }
            var amount = player.RecoveryPower * (isCriticalHit ? player.CriticalDamageMultiplier : 1f);
            player.TakeExperience(amount / 10f);
            Health += amount;
            UpdateValue();
            base.Interact(player);
        }
    }
}