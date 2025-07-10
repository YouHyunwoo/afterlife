using Afterlife.Core;
using UnityEngine;

namespace Afterlife.View
{
    public class ThunderAltar : Object
    {
        [Header("Light")]
        [SerializeField] float intensity;
        [SerializeField] float range;

        [SerializeField] float attackDamage;
        [SerializeField] float attackRange;
        [SerializeField] float attackInterval;
        [SerializeField] string Tag;

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
        }

        void Update()
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime < attackInterval) { return; }

            elapsedTime = 0f;

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            var enemies = map.FindObjectsWithCondition(o => o is Monster && o.IsAlive && o.CompareTag(Tag));
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
                Attack(closestEnemy.transform);
            }
            else
            {
                elapsedTime = attackInterval;
            }
        }

        void Attack(Transform targetTransform)
        {
            var targetObject = targetTransform.GetComponent<Object>();
            if (targetObject == null || !targetObject.IsAlive) { return; }
            targetObject.TakeDamage(attackDamage, this);
            ServiceLocator.Get<EffectManager>().PlayGFX("Thunder", targetTransform.position);
            ServiceLocator.Get<AudioManager>().PlaySFX("thunder");
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