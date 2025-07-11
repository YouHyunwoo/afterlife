using Afterlife.Core;
using UnityEngine;

namespace Afterlife.View
{
    public class Village : Object
    {
        [Header("Light")]
        [SerializeField] float intensity;
        [SerializeField] float range;

        Model.Light sight;

        protected override void Start()
        {
            base.Start();

            sight = new Model.Light
            {
                Location = Vector2Int.FloorToInt(transform.position),
                Intensity = intensity,
                Range = range,
            };

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.Fog.AddLight(sight);
            map.Fog.Invalidate();
        }

        public override void Interact(Model.Player player)
        {
            ServiceLocator.Get<EffectManager>().PlayGFX("Heal", transform.position);
            ServiceLocator.Get<AudioManager>().PlaySFX("heal");
            var isCriticalHit = UnityEngine.Random.value < player.CriticalRate;
            if (isCriticalHit) { ServiceLocator.Get<AudioManager>().PlaySFX("critical"); }
            var amount = player.RecoveryPower * (isCriticalHit ? player.CriticalDamageMultiplier : 1f);
            player.TakeExperience(amount / 10f);
            Value += amount;
            RefreshValue();
            base.Interact(player);
        }

        public override void Die()
        {
            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.Fog.RemoveLight(sight);
            map.Fog.Invalidate();

            base.Die();
        }
    }
}