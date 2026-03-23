using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenWanderState : CitizenState
    {
        private CitizenStateContext _context;
        private CitizenVisible _visible;
        private bool _isCommand;

        public CitizenWanderState(string stateId) : base(stateId)
        {
        }

        protected override void OnInitialize()
        {
            _context = stateContext as CitizenStateContext;
            _visible = _context.CitizenVisible;
        }

        protected override void OnEnter(object[] args)
        {
            if (args == null || args.Length != 1)
            {
                _isCommand = false;
                if (FindRandomDestinationInTown(out var destination))
                {
                    _visible.StartMovement(destination);
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
                _visible.StartMovement(destination);
            }
        }

        protected override void OnUpdate()
        {
            if (_visible.HasReachedDestination())
            {
                if (_isCommand)
                    Transit("idle", null, new object[] { TimeSpan.FromSeconds(10) });
                else
                    Transit("idle");
            }
        }

        private bool FindRandomDestinationInTown(out Vector3 destination)
        {
            var townAreaSystem = _context.TownAreaSystem;
            var gridSystem = _context.GridSystem;

            var positions = townAreaSystem.GetAllInfluencedPositions();

            if (positions == null || positions.Count == 0)
            {
                destination = _visible.transform.position;
                return false;
            }

            while (positions.Count > 0)
            {
                var index = UnityEngine.Random.Range(0, positions.Count);
                var candidate = positions[index];
                positions.RemoveAt(index);

                var cell = new Vector2Int(Mathf.RoundToInt(candidate.x), Mathf.RoundToInt(candidate.y));
                var size = Vector2Int.one;

                if (gridSystem.IsPassable(GridLayer.Terrain, cell, size) &&
                    gridSystem.IsPassable(GridLayer.Field, cell, size))
                {
                    destination = candidate;
                    return true;
                }
            }

            destination = _visible.transform.position;
            return false;
        }
    }
}