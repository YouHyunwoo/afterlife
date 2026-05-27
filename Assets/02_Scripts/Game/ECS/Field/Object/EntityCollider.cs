using System;
using System.Collections.Generic;
using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class EntityCollider : Moonstone.Ore.Local.Entity
    {
        private readonly HashSet<Entity> _entities = new();
        private Collider2D _collider;

        public Collider2D Collider => _collider;

        public event Action<Collider2D, EntityCollider, object> OnEnter;
        public event Action<Collider2D, EntityCollider, object> OnStay;
        public event Action<Collider2D, EntityCollider, object> OnExit;

        private void OnDisable()
        {
            _entities.Clear();
        }

        protected override void OnInitialize()
        {
            TryGetComponent(out _collider);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out Entity entity))
            {
                _entities.Add(entity);
                OnEnter?.Invoke(collision, this, this);
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
            => OnStay?.Invoke(collision, this, this);

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out Entity entity))
            {
                OnExit?.Invoke(collision, this, this);
                _entities.Remove(entity);
            }
        }

        public void Refresh()
        {
            enabled = false;
            enabled = true;
        }

        public bool IsCollided(Entity entity)
            => _entities.Contains(entity);
    }
}