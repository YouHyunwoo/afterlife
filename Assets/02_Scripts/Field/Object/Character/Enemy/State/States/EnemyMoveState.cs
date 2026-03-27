using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class EnemyMoveState : EnemyState
    {
        private float _nextScanTime;
        private Transform _currentTarget;

        public EnemyMoveState(string stateId) : base(stateId)
        {
        }

        protected override void OnEnter(object[] args)
        {
            _nextScanTime = 0f;
            if (args == null || args.Length != 1)
            {
                _currentTarget = null;
                MoveToRandomTownPosition();
            }
            else
            {
                _currentTarget = args?[0] as Transform;
                visible.StartMovement(_currentTarget.position);
            }
        }

        protected override void OnUpdate()
        {
            if (Time.time >= _nextScanTime)
            {
                _nextScanTime = Time.time + 1f;
                _currentTarget = FindPriorityTarget(visible.DetectionRange);

                if (_currentTarget != null)
                {
                    float dist = Vector2.Distance(visible.transform.position, _currentTarget.position);
                    if (dist <= visible.AttackRange)
                    {
                        Transit("fight", null, new object[] { _currentTarget });
                        return;
                    }
                }
            }

            if (_currentTarget != null)
                visible.StartMovement(_currentTarget.position);

            if (visible.HasReachedDestination() && _currentTarget == null)
            {
                MoveToRandomTownPosition();
            }
        }

        private void MoveToRandomTownPosition()
        {
            var positions = context.TownAreaSystem.GetAllInfluencedPositions();
            if (positions == null || positions.Count == 0)
                return;

            var destination = positions[Random.Range(0, positions.Count)];
            visible.StartMovement(destination);
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
