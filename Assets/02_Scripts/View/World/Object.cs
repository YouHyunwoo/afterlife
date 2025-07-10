using System;
using TMPro;
using UnityEngine;

namespace Afterlife.View
{
    public class Object : MonoBehaviour
    {
        public float Value;
        [HideInInspector]
        public float OriginalValue;

        public bool IsAlive;

        protected SpriteRenderer bodySpriteRenderer;
        protected TextMeshPro valueText;

        public event Action<Object> OnInteractedEvent;
        public event Action<Object, Object> OnHitEvent;
        public event Action<Object, Object> OnDiedEvent;

        protected virtual void Awake()
        {
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
        }

        protected void RefreshValue()
        {
            valueText.text = $"{Mathf.Max(Value, 0):0}";
        }

        public virtual void Interact(Model.Player player)
        {
            OnInteractedEvent?.Invoke(this);
        }

        public virtual void TakeDamage(float damage, Object attacker)
        {
            if (!IsAlive) { return; }

            Value -= damage;
            OnHitEvent?.Invoke(attacker, this);

            RefreshValue();

            if (Value <= 0f)
            {
                Die();
                OnDiedEvent?.Invoke(attacker, this);
            }
        }

        public virtual void Die()
        {
            IsAlive = false;
            gameObject.SetActive(false);
            Destroy(gameObject, 0);
        }
    }
}