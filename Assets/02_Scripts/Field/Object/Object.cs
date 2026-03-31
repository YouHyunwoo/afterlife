using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public record struct DamagedInfo(float ActualDamage, float Damage, Object Attacker);

    public class Object : Moonstone.Ore.Model, IDamageable
    {
        public Vector3 Position { get; set; }
        public MovementComponent Movement { get; } = new();
        public CollisionComponent Collision { get; } = new();

        protected Vector2Int size;
        protected float health, maxHealth;

        public Vector2Int Size => size;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public bool IsAlive => health > 0;

        public event Action<DamagedInfo, Object, object> OnDamaged;
        public event Action<Object, Object, object> OnDied;

        public Object(string id) : base(id) { }

        public virtual void Update(float deltaTime) { }

        public virtual void Initialize(ObjectData data)
        {
            size = data.Size;
            health = maxHealth = data.Health;
        }

        public virtual void TakeDamage(float damage, Object attacker)
        {
            ApplyDamage(damage, attacker);
            if (health <= 0f)
                Die(attacker);
        }

        protected virtual void ApplyDamage(float damage, Object attacker)
        {
            var actualDamage = Mathf.Clamp(damage, 0f, health);
            health -= actualDamage;
            OnDamaged?.Invoke(new DamagedInfo(actualDamage, damage, attacker), this, this);
        }

        protected virtual void Die(Object attacker)
        {
            OnDied?.Invoke(attacker, this, this);
        }
    }
}