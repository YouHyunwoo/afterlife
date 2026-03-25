using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenReturnState : CitizenState
    {
        private CitizenStateContext _context;
        private CitizenVisible _visible;

        private HouseVisible _nearestHouseVisible;

        public CitizenReturnState(string stateId) : base(stateId)
        {
        }

        protected override void OnInitialize()
        {
            _context = stateContext as CitizenStateContext;
            _visible = _context.CitizenVisible;
        }

        protected override void OnEnter(object[] args)
        {
            if (FindNearestHouseVisible(out _nearestHouseVisible))
            {
                _visible.OnInteractionCollided += HandleInteractionCollided;
                _visible.StartMovement(_nearestHouseVisible.transform.position);
            }
            else
            {
                _visible.ClearHoldableVisibles();
                Transit("idle");
            }
        }

        private void HandleInteractionCollided(Collider2D collider, CollisionField collisionField, CharacterVisible characterVisible, object sender)
        {
            if (_nearestHouseVisible.transform == collider.transform)
            {
                _visible.StopMovement();
                _visible.ObtainHoldables();
                Transit("idle");
            }
        }

        private bool FindNearestHouseVisible(out HouseVisible nearestHouseVisible)
            => _visible.FindNearestHouseVisible(out nearestHouseVisible);

        protected override void OnExit(object[] args)
        {
            _visible.OnInteractionCollided -= HandleInteractionCollided;
            _nearestHouseVisible = null;
        }
    }
}