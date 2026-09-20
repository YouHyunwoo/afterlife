using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class EnemyFightState : EnemyState
    {
        private Object _target;
        private float _nextScanTime;
        private float _nextAttackTime;
        private float _returnDelayTimer;

        public EnemyFightState(string stateId) : base(stateId) { }

        protected override void OnEnter(object[] args)
        {
            model.Movement.ShouldStop = true;
            _target = args?[0] as Object;
            _nextScanTime = Time.time + 1f;
            _nextAttackTime = Time.time + model.AttackInterval;
            _returnDelayTimer = -1f;
        }

        protected override void OnUpdate()
        {
            if (Time.time >= _nextScanTime)
            {
                _nextScanTime = Time.time + 1f;
                var best = model.TargetScan.FindPriorityTarget(model.Position, model.DetectionRange, model);
                if (best != null && best != _target)
                    _target = best;
            }

            bool hasValidTarget = _target != null
                && _target.IsAlive
                && Vector2.Distance(model.Position, _target.Position) <= model.AttackRange;

            if (!hasValidTarget)
            {
                if (_returnDelayTimer < 0f)
                    _returnDelayTimer = Time.time + 0.5f;
                else if (Time.time >= _returnDelayTimer)
                    Transit("move", null, new object[] { _target });
                return;
            }

            _returnDelayTimer = -1f;

            if (Time.time >= _nextAttackTime)
            {
                _target.TakeDamage(model.AttackPower, model);
                _nextAttackTime = Time.time + model.AttackInterval;
            }
        }

        protected override void OnExit(object[] args)
        {
            _target = null;
        }
    }
}
