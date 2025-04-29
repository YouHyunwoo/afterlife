using System;
using TMPro;
using UnityEngine;

namespace Afterlife.View
{
    public class Object : MonoBehaviour
    {
        public float Health;
        public Model.Map Map;

        TextMeshPro text;

        public event Action OnDied;

        protected virtual void Awake()
        {
            text = GetComponentInChildren<TextMeshPro>();
        }

        protected virtual void Start()
        {
            UpdateValue();
        }

        protected void UpdateValue()
        {
            if (text == null) { return; }
            text.text = $"{Health:0}";
        }

        public virtual void Interact(Model.Player player)
        {
            TakeDamage(player.AttackPower, null);
        }

        public virtual void TakeDamage(float damage, Object @object)
        {
            Health -= damage;
            if (Health <= 0f)
            {
                Died();
                OnDied?.Invoke();
            }
            else
            {
                UpdateValue();
            }
        }

        public virtual void Died()
        {
            var location = Vector2Int.FloorToInt(transform.position);
            Map.Field.Set(location, null);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}