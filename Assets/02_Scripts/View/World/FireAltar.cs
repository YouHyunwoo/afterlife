using Afterlife.Core;
using UnityEngine;

namespace Afterlife.View
{
    [System.Serializable]
    public class UpgradeInformation
    {
        public float Value;
        public float LightIntensity;
        public float LightRange;
        public float AttackDamage;
        public float AttackRange;
        public float AttackInterval;
        public float ProjectileSpeed;
    }

    public class FireAltar : Object
    {
        [Header("Light")]
        [SerializeField] float intensity;
        [SerializeField] float range;

        [SerializeField] float attackDamage;
        [SerializeField] float attackRange;
        [SerializeField] float attackInterval;
        [SerializeField] float projectileSpeed = 5f;
        [SerializeField] Projectile ProjectilePrefab;
        [SerializeField] string Tag;

        [Header("Interaction")]
        [SerializeField] string requiredItemName;
        [SerializeField] int requiredItemAmount;

        [Header("Upgrade Information")]
        [SerializeField] UpgradeInformation[] upgradeInformation;

        Model.Light sight;
        float elapsedTime;

        protected override void Start()
        {
            base.Start();

            sight = new Model.Light
            {
                Location = new Vector2Int((int)transform.position.x, (int)transform.position.y),
                Intensity = intensity,
                Range = range,
            };

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.Fog.AddLight(sight);
            map.Fog.Invalidate();

            elapsedTime = attackInterval;
        }

        void Update()
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime < attackInterval) { return; }

            elapsedTime = 0f;

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            var enemies = map.FindObjectsWithCondition(o => o.IsAlive && o.CompareTag(Tag));
            var minDistance = float.MaxValue;
            var closestEnemy = (Object)null;
            foreach (var enemy in enemies)
            {
                var distance = Vector2.Distance(enemy.transform.position, transform.position);
                if (distance <= attackRange && distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy != null)
            {
                FireProjectile(closestEnemy.transform);
            }
            else
            {
                elapsedTime = attackInterval;
            }
        }

        void FireProjectile(Transform targetTransform)
        {
            var projectile = Instantiate(ProjectilePrefab, transform.position + new Vector3(0.5f, 0.5f), Quaternion.identity);
            projectile.Owner = this;
            projectile.Target = targetTransform.GetComponent<Object>();
            projectile.TargetPosition = targetTransform.position + new Vector3(0.5f, 0.5f);
            projectile.Damage = attackDamage;
            projectile.Speed = projectileSpeed;
            ServiceLocator.Get<AudioManager>().PlaySFX("enter-mission");
        }

        public override void Interact(Model.Player player)
        {
            if (!player.Inventory.ContainsKey(requiredItemName)) { return; }

            if (player.Inventory[requiredItemName] < requiredItemAmount) { return; }

            player.Inventory[requiredItemName] -= requiredItemAmount;
            if (player.Inventory[requiredItemName] <= 0)
            {
                player.Inventory.Remove(requiredItemName);
            }

            Value += 1f;
            RefreshValue();
            UpdateStatistics();

            base.Interact(player);
        }

        public override void TakeDamage(float damage, Object attacker)
        {
            base.TakeDamage(damage, attacker);
            if (!IsAlive) { return; }
            UpdateStatistics();
        }

        void UpdateStatistics()
        {
            for (var i = upgradeInformation.Length - 1; i >= 0; i--)
            {
                if (Value < upgradeInformation[i].Value) { continue; }

                sight.Intensity = upgradeInformation[i].LightIntensity;
                sight.Range = upgradeInformation[i].LightRange;
                attackDamage = upgradeInformation[i].AttackDamage;
                attackRange = upgradeInformation[i].AttackRange;
                attackInterval = upgradeInformation[i].AttackInterval;
                projectileSpeed = upgradeInformation[i].ProjectileSpeed;
                break;
            }
        }

        public override void Die()
        {
            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.Fog.RemoveLight(sight);
            map.Fog.Invalidate();

            base.Die();
        }
    }
}