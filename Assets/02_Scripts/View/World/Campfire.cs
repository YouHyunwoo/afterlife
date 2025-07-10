using System.Collections;
using UnityEngine;

namespace Afterlife.View
{
    public class Campfire : Object
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
                Location = new Vector2Int((int)transform.position.x, (int)transform.position.y),
                Intensity = intensity,
                Range = range,
            };

            Map.Fog.AddLight(sight);
            Map.Fog.Update();

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
            if (!player.Inventory.ContainsKey("wood")) { return; }
            if (player.Inventory["wood"] <= 0) { return; }

            player.Inventory["wood"]--;
            if (player.Inventory["wood"] <= 0)
            {
                player.Inventory.Remove("wood");
            }

            Health += 1f;
            UpdateValue();
            base.Interact(player);
        }

        public override void Died()
        {
            Map.Fog.RemoveLight(sight);
            Map.Fog.Update();

            base.Died();
        }
    }
}