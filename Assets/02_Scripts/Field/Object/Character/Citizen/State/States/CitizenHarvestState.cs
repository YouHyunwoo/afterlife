using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenHarvestState : CitizenState
    {
        private CitizenStateContext _context;
        private CitizenVisible _visible;

        private ResourceVisible _targetResourceVisible;
        private bool _isAttached;

        public CitizenHarvestState(string stateId) : base(stateId)
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
                Transit("idle");
            }
            else
            {
                _visible.OnInteractionCollided += HandleInteractionCollided;
                _visible.RefreshInteractionCollisionField();

                _targetResourceVisible = (ResourceVisible)args[0];
                _visible.StartMovement(_targetResourceVisible.transform.position);
            }
        }

        protected override void OnExit(object[] args)
        {
            if (_isAttached)
            {
                _targetResourceVisible.DetachCitizen(_visible);
                _isAttached = false;
            }
            _targetResourceVisible.OnHarvested -= HandleHarvested;
            _visible.OnInteractionCollided -= HandleInteractionCollided;
        }

        private void HandleInteractionCollided(Collider2D collider, CollisionField collisionField, CharacterVisible characterVisible, object sender)
        {
            if (_targetResourceVisible.transform == collider.transform)
            {
                if (!_targetResourceVisible.IsHarvested)
                {
                    _visible.StopMovement();
                    _targetResourceVisible.OnHarvested += HandleHarvested;
                    _isAttached = true;
                    _targetResourceVisible.AttachCitizen(_visible);
                }
            }
        }

        private void HandleHarvested(HoldableVisible holdableVisible, ResourceVisible resourceVisible, object sender)
        {
            _visible.AddHoldableVisible(holdableVisible);
            Transit("return");
        }
    }
}