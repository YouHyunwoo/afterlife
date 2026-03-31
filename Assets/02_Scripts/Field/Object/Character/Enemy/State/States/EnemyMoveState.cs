using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class EnemyMoveState : EnemyState
    {
        private Object _target;
        private float _nextScanTime;

        public EnemyMoveState(string stateId) : base(stateId) { }

        protected override void OnEnter(object[] args)
        {
            if (args == null || args.Length != 1)
            {
                _target = null;
                MoveToRandomTownPosition();
            }
            else
            {
                _target = (Object)args[0];
                model.Movement.Destination = _target.Position;
            }

            _nextScanTime = 0f;
        }

        protected override void OnUpdate()
        {
            if (Time.time >= _nextScanTime)
            {
                _nextScanTime = Time.time + 1f;
                var newTarget = model.TargetScan.FindPriorityTarget(model.Position, model.DetectionRange, model);

                if (newTarget != null)
                {
                    _target = newTarget;
                    float dist = Vector2.Distance(model.Position, _target.Position);
                    if (dist <= model.AttackRange)
                    {
                        Transit("fight", null, new object[] { _target });
                        return;
                    }
                }
            }

            if (_target != null)
                model.Movement.Destination = _target.Position;

            if (model.Movement.HasArrived && _target == null)
                MoveToRandomTownPosition();
        }

        private void MoveToRandomTownPosition()
        {
            if (model.Wander.SampleTownZonePosition(out var destination))
                model.Movement.Destination = destination;
        }
    }
}
