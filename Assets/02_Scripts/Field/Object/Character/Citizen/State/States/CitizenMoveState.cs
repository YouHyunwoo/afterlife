using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenMoveState : CitizenState
    {
        private bool _isCommand;

        public CitizenMoveState(string stateId) : base(stateId) { }

        protected override void OnEnter(object[] args)
        {
            if (args == null || args.Length != 1)
            {
                _isCommand = false;
                if (model.Wander.FindRandomDestination(out var destination))
                {
                    model.Movement.Destination = destination;
                }
                else
                {
                    Transit("idle");
                }
            }
            else
            {
                _isCommand = true;
                var destination = (Vector3)args[0];
                model.Movement.Destination = destination;
            }
        }

        protected override void OnUpdate()
        {
            if (model.Movement.HasArrived)
            {
                if (_isCommand)
                    Transit("idle", null, new object[] { TimeSpan.FromSeconds(10) });
                else
                    Transit("idle");
            }
        }
    }
}