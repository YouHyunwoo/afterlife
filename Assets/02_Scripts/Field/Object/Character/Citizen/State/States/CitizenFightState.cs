using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenFightState : CitizenState
    {
        private Enemy _target;
        private float _nextAttackTime;

        public CitizenFightState(string stateId) : base(stateId) { }

        protected override void OnEnter(object[] args)
        {
            _target = (Enemy)args[0];
            model.Movement.ShouldStop = true;
            _nextAttackTime = Time.time + model.AttackInterval;
        }

        protected override void OnUpdate()
        {
            if (_target == null || !_target.IsAlive)
            {
                Transit("idle");
                return;
            }

            var distance = Vector3.Distance(model.Position, _target.Position);
            if (distance > model.AttackRange)
            {
                Transit("chase", null, new object[] { _target });
                return;
            }

            if (Time.time >= _nextAttackTime)
            {
                _target.TakeDamage(model.AttackPower, model);
                _nextAttackTime = Time.time + model.AttackInterval;
            }
        }
    }
}