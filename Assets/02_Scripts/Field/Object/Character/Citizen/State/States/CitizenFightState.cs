using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenFightState : CitizenState
    {
        private EnemyVisible _targetVisible;
        private float _nextAttackTime;

        public CitizenFightState(string stateId) : base(stateId)
        {
        }

        protected override void OnEnter(object[] args)
        {
            if (args == null || args.Length != 1)
            {
                Transit("idle");
            }
            else
            {
                visible.StopMovement();
                _targetVisible = (EnemyVisible)args[0];
                _nextAttackTime = Time.time + visible.AttackInterval;
            }
        }

        protected override void OnUpdate()
        {
            if (_targetVisible == null
                || !_targetVisible.gameObject.activeInHierarchy)
            {
                Transit("idle");
                return;
            }

            bool hasValidTarget = Vector2.Distance(visible.transform.position, _targetVisible.transform.position) <= visible.AttackRange;

            if (!hasValidTarget)
            {
                Transit("move");
                return;
            }

            if (Time.time >= _nextAttackTime)
            {
                Debug.Log("dd");
                Attack(_targetVisible.transform);
                _nextAttackTime = Time.time + visible.AttackInterval;
            }
        }

        private void Attack(Transform target)
        {
            if (target.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(visible.AttackPower, null);
        }
    }
}