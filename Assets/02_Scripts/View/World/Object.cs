using System;
using System.Collections;
using System.Collections.Generic;
using Afterlife.Core;
using DG.Tweening;
using TMPro;
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

    public enum ObjectDirection
    {
        Left, Right, Up, Down
    }

    public class Object : MonoBehaviour
    {
        public float Value;
        [HideInInspector]
        public float OriginalValue;

        [SerializeField] bool isSightEnabled;
        public Model.Light Sight;
        public bool IsAlive;
        public List<ItemDropGroup> Loot;
        public bool IsDeadAnimationEnabled;

        protected ObjectDirection direction;

        protected Animator animator;
        protected SpriteRenderer bodySpriteRenderer;
        protected TextMeshPro valueText;
        protected Sequence disappearSequence;

        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;

        public event Action<Object> OnInteractedEvent;
        public event Action<float, Object, Object> OnHitEvent;
        public event Action<Object, Object> OnDiedEvent;
        public event Action<Object> OnDisposingEvent;

        protected virtual void Awake()
        {
            animator = GetComponent<Animator>();

            var bodyObject = transform.Find("Body");
            bodySpriteRenderer = bodyObject.GetComponent<SpriteRenderer>();

            var valueTextObject = transform.Find("Value").Find("Text");
            valueText = valueTextObject.GetComponent<TextMeshPro>();

            OriginalValue = Value;
            IsAlive = true;
        }

        protected virtual void Start()
        {
            RefreshValue();
            var randomInitialDirection = (ObjectDirection)UnityEngine.Random.Range(0, 4);
            SetDirection(randomInitialDirection);
            InitializeSight();
        }

        protected void RefreshValue() => valueText.text = $"{Mathf.Max(Value, 0):0}";

        public void SetDirection(ObjectDirection direction)
        {
            this.direction = direction;
            switch (direction)
            {
                case ObjectDirection.Left:
                    bodySpriteRenderer.flipX = true;
                    break;
                case ObjectDirection.Right:
                    bodySpriteRenderer.flipX = false; // default
                    break;
                case ObjectDirection.Up:
                    break;
                case ObjectDirection.Down:
                    break;
            }
        }

        void InitializeSight()
        {
            if (!isSightEnabled) { return; }
            Sight.Location = new Vector2Int((int)transform.position.x, (int)transform.position.y);

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.Fog.AddLight(Sight);
            map.Fog.Invalidate();
        }

        public virtual void RefreshSight()
        {
            if (!isSightEnabled) { return; }
            Sight.Location = new Vector2Int((int)transform.position.x, (int)transform.position.y);

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.Fog.Invalidate();
        }

        public virtual void Interact(Model.Player player) => OnInteractedEvent?.Invoke(this);

        public virtual void TakeDamage(float damage, Object attacker)
        {
            if (!IsAlive) { return; }

            var actualDamage = Mathf.Min(damage, Value);
            Hit(actualDamage, attacker);
            if (Value <= 0f) { Die(attacker); }
        }

        protected virtual void Hit(float damage, Object attacker)
        {
            Value -= damage;
            animator.SetTrigger("Hit");
            RefreshValue();
            OnHitEvent?.Invoke(damage, attacker, this);
        }

        public virtual void Die(Object attacker)
        {
            IsAlive = false;
            FinalizeSight();
            if (IsDeadAnimationEnabled) { animator.SetBool("Dead", true); }
            else { Disappear(); }
            if (Loot.Count > 0) { StartCoroutine(CollectItemsByKillRoutine()); }
            OnDiedEvent?.Invoke(attacker, this);
        }

        void FinalizeSight()
        {
            if (!isSightEnabled) { return; }
            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.Fog.RemoveLight(Sight);
            map.Fog.Invalidate();
        }

        public void Disappear()
        {
            disappearSequence = DOTween.Sequence()
                .Append(valueText.DOFade(0f, 0.5f))
                .Join(bodySpriteRenderer.DOFade(0f, 1f))
                .OnComplete(() => Dispose());
        }

        protected virtual void Dispose()
        {
            OnDisposingEvent?.Invoke(this);
            disappearSequence.Kill();
            disappearSequence = null;
            gameObject.SetActive(false);
            Destroy(gameObject, 0);
        }

        protected IEnumerator CollectItemsByKillRoutine()
        {
            var stageManager = ServiceLocator.Get<StageManager>();
            if (stageManager == null) { yield break; }

            var itemCollectSystem = stageManager.itemCollectSystem;

            foreach (var itemDropGroup in Loot)
            {
                yield return new WaitForSeconds(0.3f);

                var itemId = itemDropGroup.Id;
                var itemAmount = Mathf.FloorToInt(itemDropGroup.Amount * OriginalValue / 10f);
                var itemDropRate = itemDropGroup.DropRate;
                var itemActualAmount = itemCollectSystem.SampleItems(itemAmount, itemDropRate);
                if (itemActualAmount <= 0) { continue; }
                itemCollectSystem.CollectWithRate(itemId, itemActualAmount);
                itemCollectSystem.ShowPopup(transform.position + new Vector3(.5f, .5f), itemId, itemActualAmount);
            }
        }
    }
}