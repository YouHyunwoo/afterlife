using DG.Tweening;
using UnityEngine;

namespace Afterlife.View
{
    public class Resource : Object
    {
        public string Type;
        public int Amount;

        public SpriteRenderer SpriteRenderer;

        protected override void Awake()
        {
            base.Awake();

            SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public override void Interact(Model.Player player)
        {
            void OnDiedEvent(Object attacker, Object @object)
            {
                var inventory = player.Inventory;
                if (!inventory.ContainsKey(Type)) { inventory[Type] = 0; }
                player.Inventory[Type] += Amount;
                Debug.Log($"Collected {Amount} of {Type}. Total: {player.Inventory[Type]}");
            }

            OnDied += OnDiedEvent;
            TakeDamage(player.AttackPower, null);
            base.Interact(player);
            OnDied -= OnDiedEvent;

            if (Health > 0f)
            {
                var bodyTransform = transform.Find("Body");
                bodyTransform.DOShakePosition(0.2f, 0.1f, 30, 90, false, true);
            }
        }

        public override void Died()
        {
            SpriteRenderer.DOFade(0f, 1f).OnComplete(() =>
            {
                base.Died();
            });
        }
    }
}