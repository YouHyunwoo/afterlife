using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenChaseState : CitizenState
    {
        private EnemyVisible _targetVisible;

        public CitizenChaseState(string stateId) : base(stateId)
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
                _targetVisible = (EnemyVisible)args[0];
                visible.StartMovement(_targetVisible.transform.position);
            }
        }

        protected override void OnUpdate()
        {
            if (_targetVisible != null)
            {
                float dist = Vector2.Distance(visible.transform.position, _targetVisible.transform.position);
                if (dist <= visible.AttackRange)
                {
                    Transit("fight", null, new object[] { _targetVisible });
                    return;
                }
            }

            if (_targetVisible != null)
                visible.StartMovement(_targetVisible.transform.position);
        }
    }
}