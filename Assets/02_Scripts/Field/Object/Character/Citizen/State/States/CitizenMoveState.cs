using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenMoveState : CitizenState
    {
        private bool _isCommand;

        public CitizenMoveState(string stateId) : base(stateId)
        {
        }

        protected override void OnEnter(object[] args)
        {
            if (args == null || args.Length != 1)
            {
                _isCommand = false;
                if (FindRandomDestinationInTown(out var destination))
                {
                    visible.StartMovement(destination);
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
                visible.StartMovement(destination);
            }
        }

        protected override void OnUpdate()
        {
            if (visible.HasReachedDestination())
            {
                if (_isCommand)
                    Transit("idle", null, new object[] { TimeSpan.FromSeconds(10) });
                else
                    Transit("idle");
            }
        }

        private bool FindRandomDestinationInTown(out Vector3 destination)
        {
            var townAreaSystem = context.TownAreaSystem;
            var gridSystem = context.GridSystem;

            var positions = townAreaSystem.GetAllInfluencedPositions();

            if (positions == null || positions.Count == 0)
            {
                destination = visible.transform.position;
                return false;
            }

            while (positions.Count > 0)
            {
                var index = UnityEngine.Random.Range(0, positions.Count);
                var candidate = positions[index];
                positions.RemoveAt(index);

                var cell = new Vector2Int(Mathf.RoundToInt(candidate.x), Mathf.RoundToInt(candidate.y));
                var size = Vector2Int.one;

                if (gridSystem.IsPassable(cell, size))
                {
                    destination = candidate;
                    return true;
                }
            }

            destination = visible.transform.position;
            return false;
        }
    }
}