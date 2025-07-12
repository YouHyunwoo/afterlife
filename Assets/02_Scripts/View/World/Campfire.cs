using System.Collections;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.View
{
    public class Campfire : Object
    {
        [Header("Light")]
        [SerializeField] float intensity;
        [SerializeField] float range;

        [Header("Interaction")]
        [SerializeField] string requiredItemName;
        [SerializeField] int requiredItemAmount;

        Model.Light sight;

        protected override void Start()
        {
            base.Start();

            sight = new Model.Light
            {
                Location = new Vector2Int((int)transform.position.x, (int)transform.position.y),
                Intensity = intensity,
                Range = range,
            };

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.Fog.AddLight(sight);
            map.Fog.Invalidate();

            StartCoroutine(GoOutRoutine());
        }

        IEnumerator GoOutRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                sight.Intensity -= 0.1f;
                TakeDamage(1f, this);
            }
        }

        public override void Interact(Model.Player player)
        {
            var inventory = player.Inventory;

            if (!inventory.HasItem(requiredItemName, requiredItemAmount)) { return; }
            inventory.RemoveItem(requiredItemName, requiredItemAmount, out var _);

            Value += 1f;
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