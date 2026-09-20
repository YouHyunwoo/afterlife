using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenChaseState : CitizenState
    {
        private Enemy _target;

        public CitizenChaseState(string stateId) : base(stateId) { }

        protected override void OnEnter(object[] args)
        {
            _target = (Enemy)args[0];
            model.Movement.Destination = _target.Position;
        }

        protected override void OnUpdate()
        {
            if (_target == null)
            {
                Transit("idle");
                return;
            }

            float distance = Vector3.Distance(model.Position, _target.Position);
            if (distance <= model.AttackRange * 0.95f)
            {
                Transit("fight", null, new object[] { _target });
                return;
            }

            model.Movement.Destination = _target.Position;
        }
    }
}