using System.Collections;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.View
{
    public class Campfire : Object
    {
        [Header("Campfire")]
        [SerializeField] float decreaseSpeed = 0.1f;

        [Header("Interaction")]
        [SerializeField] string requiredItemName;
        [SerializeField] int requiredItemAmount;

        protected override void Start()
        {
            base.Start();

            StartCoroutine(GoOutRoutine());
        }

        IEnumerator GoOutRoutine()
        {
            var waiting = new WaitForSeconds(1f / decreaseSpeed);

            while (true)
            {
                yield return waiting;
                TakeDamage(1f, this);
                UpdateSight();
            }
        }

        public override void Interact(Model.Player player)
        {
            var inventory = player.Inventory;

            if (!inventory.HasItem(requiredItemName, requiredItemAmount)) { return; }
            inventory.RemoveItem(requiredItemName, requiredItemAmount, out var _);

            Value += 1f;
            RefreshValue();
            UpdateSight();

            base.Interact(player);
        }

        void UpdateSight()
        {
            Sight.Intensity = Mathf.Clamp((Value + 10) / 10f * 0.5f, 2f, 20f);
            Sight.Range = Mathf.Clamp((Value + 10) / 4f * 0.5f, 5f, 50f);

            ServiceLocator.Get<StageManager>().Stage.Map.Fog.Invalidate();
        } 
    }
}