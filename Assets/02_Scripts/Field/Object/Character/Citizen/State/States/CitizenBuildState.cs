using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenBuildState : CitizenState
    {
        private CitizenStateContext _context;
        private CitizenVisible _visible;

        private BuildingVisible _targetVisible;
        private bool _isAttached;

        public CitizenBuildState(string stateId) : base(stateId)
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

                _targetVisible = (BuildingVisible)args[0];
                _targetVisible.OnBuilt += HandleBuilt;
                _visible.StartMovement(_targetVisible.transform.position);
            }
        }

        protected override void OnExit(object[] args)
        {
            if (_isAttached)
            {
                _targetVisible.DetachCitizen(_visible);
                _isAttached = false;
            }
            _targetVisible.OnBuilt -= HandleBuilt;
            _visible.OnInteractionCollided -= HandleInteractionCollided;
        }

        private void HandleInteractionCollided(Collider2D collider, CollisionField collisionField, CharacterVisible characterVisible, object sender)
        {
            if (_targetVisible.transform == collider.transform)
            {
                if (!_targetVisible.IsBuilt)
                {
                    _visible.StopMovement();
                    _isAttached = true;
                    _targetVisible.AttachCitizen(_visible);
                }
            }
        }

        private void HandleBuilt(BuildingVisible buildingVisible, object sender)
        {
            _visible.StopMovement();
            Transit("idle");
        }
    }
}