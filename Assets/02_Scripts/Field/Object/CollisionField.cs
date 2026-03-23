using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CollisionField : Moonstone.Ore.Local.Entity
    {
        private Collider2D _collider;

        public Collider2D Collider => _collider;

        public event Action<Collider2D, CollisionField, object> OnEnter;
        public event Action<Collider2D, CollisionField, object> OnStay;
        public event Action<Collider2D, CollisionField, object> OnExit;

        protected override void OnInitialize()
        {
            TryGetComponent(out _collider);
        }

        private void OnTriggerEnter2D(Collider2D collision)
            => OnEnter?.Invoke(collision, this, this);

        private void OnTriggerStay2D(Collider2D collision)
            => OnStay?.Invoke(collision, this, this);

        private void OnTriggerExit2D(Collider2D collision)
            => OnExit?.Invoke(collision, this, this);
    }
}