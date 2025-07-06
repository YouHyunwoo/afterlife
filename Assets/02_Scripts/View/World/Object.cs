using System;
using TMPro;
using UnityEngine;

namespace Afterlife.View
{
    public class Object : MonoBehaviour
    {
        public float Health;
        public float MaxHealth;
        public bool IsAlive;
        public Model.Map Map;

        TextMeshPro text;

        public event Action<Object> OnInteractedEvent;
        public event Action<Object, Object> OnHitEvent;
        public event Action<Object, Object> OnDiedEvent;

        protected virtual void Awake()
        {
            text = GetComponentInChildren<TextMeshPro>();
        }

        protected virtual void Start()
        {
            IsAlive = true;
            UpdateValue();
        }

        protected void UpdateValue()
        {
            if (text == null) { return; }
            text.text = $"{Mathf.Max(Health, 0):0}";
        }

        public virtual void Interact(Model.Player player)
        {
            OnInteractedEvent?.Invoke(this);
        }

        public virtual void TakeDamage(float damage, Object attacker)
        {
            if (!IsAlive) { return; }

            Health -= damage;
            OnHitEvent?.Invoke(attacker, this);

            UpdateValue();

            if (Health <= 0f)
            {
                Died();
                OnDiedEvent?.Invoke(attacker, this);
            }
        }

        public virtual void Died()
        {
            IsAlive = false;
            var location = Vector2Int.FloorToInt(transform.position);
            Map.Field.Set(location, null);
            gameObject.SetActive(false);
            Destroy(gameObject, 0);
        }
    }
}