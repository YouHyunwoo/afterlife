using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenWanderState : CitizenState
    {
        private CitizenStateContext _context;
        private CitizenVisible _visible;

        public CitizenWanderState(string stateId) : base(stateId)
        {
        }

        protected override void OnInitialize()
        {
            _context = stateContext as CitizenStateContext;
            _visible = _context.CitizenVisible;
        }

        protected override void OnEnter()
        {
            if (FindRandomDestinationInTown(out var destination))
            {
                _visible.StartMovement(destination);
            }
            else
            {
                Transit("idle");
            }
        }

        protected override void OnUpdate()
        {
            if (_visible.HasReachedDestination())
            {
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
                var index = Random.Range(0, positions.Count);
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