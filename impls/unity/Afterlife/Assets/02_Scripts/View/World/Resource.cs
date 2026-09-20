using Afterlife.Core;
using DG.Tweening;
using UnityEngine;

namespace Afterlife.View
{
    public class Resource : Object
    {
        public override void Interact(Model.Player player)
        {
            ServiceLocator.Get<EffectManager>().PlayGFX("Cut", transform.position);
            ServiceLocator.Get<AudioManager>().PlaySFX("sword");
            var isCriticalHit = UnityEngine.Random.value < player.CriticalRate;
            if (isCriticalHit) { ServiceLocator.Get<AudioManager>().PlaySFX("critical"); }
            var damage = player.AttackPower * (isCriticalHit ? player.CriticalDamageMultiplier : 1f);
            player.TakeExperience(damage / 10f);
            TakeDamage(damage, null);
            CollectByInteraction(player);
            base.Interact(player);

            if (Value > 0f)
            {
                var bodyTransform = transform.Find("Body");
                bodyTransform.DOShakePosition(0.2f, 0.1f, 30, 90, false, true);
            }
        }

        void CollectByInteraction(Model.Player player)
        {
            var itemCollectSystem = ServiceLocator.Get<StageManager>().itemCollectSystem;

            foreach (var itemDropGroup in Loot)
            {
                var itemId = itemDropGroup.Id;
                var itemAmount = Mathf.FloorToInt(player.AttackPower);
                var itemDropRate = itemDropGroup.DropRate;
                var itemActualAmount = itemCollectSystem.SampleItems(itemAmount, itemDropRate);
                if (itemActualAmount <= 0) { continue; }
                itemCollectSystem.CollectWithRate(itemId, itemActualAmount);
                itemCollectSystem.ShowPopup(transform.position + new Vector3(.5f, .5f), itemId, itemActualAmount);
            }
        }
    }
}