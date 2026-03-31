using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class EnemyVisible : CharacterVisible<Enemy>
    {
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
#if UNITY_EDITOR
            var stateName = @object.StateName;
            UnityEditor.Handles.Label(transform.position, $"State: {stateName}");
#endif
            Gizmos.color = new Color(1, 0, 0, 0.4f);
            Gizmos.DrawSphere(transform.position, Object.AttackRange);
        }

        public override void Bind(Enemy enemy)
        {
            base.Bind(enemy);
            navMeshAgent.speed = enemy.MoveSpeed;
        }
    }
}