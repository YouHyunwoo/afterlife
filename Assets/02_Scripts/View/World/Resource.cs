using System;
using System.Collections;
using Afterlife.Core;
using Afterlife.GameSystem.Stage;
using DG.Tweening;
using UnityEngine;

namespace Afterlife.View
{
    [Serializable]
    public class ItemDropGroup
    {
        public string Id;
        public float DropRate;
        public int Amount;
    }

    public class Resource : Object
    {
        public string Type;
        public int Amount;
        public ItemDropGroup[] ItemDropGroups;

        public SpriteRenderer SpriteRenderer;

        protected override void Awake()
        {
            base.Awake();

            SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public override void Interact(Model.Player player)
        {
            ServiceLocator.Get<EffectManager>().PlayGFX("Cut", transform.position);
            ServiceLocator.Get<AudioManager>().PlaySFX("sword");
            TakeDamage(player.AttackPower, null);
            CollectByInteraction(player);
            base.Interact(player);

            if (Health > 0f)
            {
                var bodyTransform = transform.Find("Body");
                bodyTransform.DOShakePosition(0.2f, 0.1f, 30, 90, false, true);
            }
        }

        IEnumerator CollectByKillRoutine()
        {
            var itemCollectSystem = ServiceLocator.Get<ItemCollectSystem>();

            foreach (var itemDropGroup in ItemDropGroups)
            {
                yield return new WaitForSeconds(0.3f);

                var itemId = itemDropGroup.Id;
                var itemAmount = Mathf.FloorToInt(itemDropGroup.Amount * MaxHealth / 10f);
                var itemDropRate = itemDropGroup.DropRate;
                var itemActualAmount = itemCollectSystem.SampleItems(itemAmount, itemDropRate);
                if (itemActualAmount <= 0) { continue; }
                itemCollectSystem.CollectWithRate(itemId, itemActualAmount);
                itemCollectSystem.ShowPopup(transform.position + new Vector3(.5f, .5f), itemId, itemActualAmount);
            }
        }

        void CollectByInteraction(Model.Player player)
        {
            var itemCollectSystem = ServiceLocator.Get<ItemCollectSystem>();

            foreach (var itemDropGroup in ItemDropGroups)
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

        public override void Died()
        {
            IsAlive = false;
            var location = Vector2Int.FloorToInt(transform.position);
            Map.Field.Set(location, null);

            StartCoroutine(CollectByKillRoutine());

            SpriteRenderer.DOFade(0f, 1f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                Destroy(gameObject, 0);
            });
        }
    }
}