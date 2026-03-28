using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class EnemyFightState : EnemyState
    {
        private Transform _currentTarget;
        private float _nextScanTime;
        private float _nextAttackTime;
        private float _returnDelayTimer;

        public EnemyFightState(string stateId) : base(stateId)
        {
        }

        protected override void OnEnter(object[] args)
        {
            visible.StopMovement();
            _currentTarget = args?[0] as Transform;
            _nextScanTime = Time.time + 1f;
            _nextAttackTime = Time.time + visible.AttackInterval;
            _returnDelayTimer = -1f;
        }

        protected override void OnUpdate()
        {
            if (Time.time >= _nextScanTime)
            {
                _nextScanTime = Time.time + 1f;
                var best = FindPriorityTarget(visible.DetectionRange);
                if (best != null && best != _currentTarget)
                {
                    _currentTarget = best;
                }
            }

            bool hasValidTarget = _currentTarget != null
                && _currentTarget.gameObject.activeInHierarchy
                && Vector2.Distance(visible.transform.position, _currentTarget.position) <= visible.AttackRange;

            if (!hasValidTarget)
            {
                if (_returnDelayTimer < 0f)
                {
                    _returnDelayTimer = Time.time + 0.5f;
                }
                else if (Time.time >= _returnDelayTimer)
                {
                    Transit("move", null, null);
                }
                return;
            }

            _returnDelayTimer = -1f;

            if (Time.time >= _nextAttackTime)
            {
                Attack(_currentTarget);
                _nextAttackTime = Time.time + visible.AttackInterval;
            }
        }

        protected override void OnExit(object[] args)
        {
            _currentTarget = null;
        }

        private void Attack(Transform target)
        {
            if (target.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(visible.AttackPower, visible);
        }

        private Transform FindPriorityTarget(float range)
        {
            var hits = Physics2D.OverlapCircleAll(visible.transform.position, range);

            Transform best1st = null;
            Transform best2nd = null;
            float dist1st = float.MaxValue;
            float dist2nd = float.MaxValue;

            foreach (var col in hits)
            {
                if (col.transform == visible.transform)
                    continue;

                float d = Vector2.Distance(visible.transform.position, col.transform.position);

                // 1순위: 시민, 미끼 장애물, 화살탑
                // TODO: BaitObstacleVisible, ArrowTowerVisible 추가 시 조건 추가
                bool is1st = col.TryGetComponent<CitizenVisible>(out _);

                // 2순위: 기타 건물
                bool is2nd = !is1st && col.TryGetComponent<BuildingVisible>(out _);

                if (is1st && d < dist1st) { best1st = col.transform; dist1st = d; }
                else if (is2nd && d < dist2nd) { best2nd = col.transform; dist2nd = d; }
            }

            return best1st ?? best2nd;
        }
    }
}
