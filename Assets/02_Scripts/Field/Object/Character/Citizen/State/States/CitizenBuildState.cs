using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenBuildState : CitizenState
    {
        private BuildingVisible _targetVisible;
        private bool _isAttached;

        public CitizenBuildState(string stateId) : base(stateId)
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
                visible.OnInteractionCollided += HandleInteractionCollided;
                visible.RefreshInteractionCollisionField();

                _targetVisible = (BuildingVisible)args[0];
                _targetVisible.OnBuilt += HandleBuilt;
                visible.StartMovement(_targetVisible.transform.position);
            }
        }

        protected override void OnExit(object[] args)
        {
            if (_isAttached)
            {
                _targetVisible.DetachCitizen(visible);
                _isAttached = false;
            }
            _targetVisible.OnBuilt -= HandleBuilt;
            visible.OnInteractionCollided -= HandleInteractionCollided;
        }

        private void HandleInteractionCollided(Collider2D collider, CollisionField collisionField, CharacterVisible characterVisible, object sender)
        {
            if (_targetVisible.transform == collider.transform)
            {
                if (!_targetVisible.IsBuilt)
                {
                    visible.StopMovement();
                    _isAttached = true;
                    _targetVisible.AttachCitizen(visible);
                }
            }
        }

        private void HandleBuilt(BuildingVisible buildingVisible, object sender)
        {
            visible.StopMovement();
            Transit("idle");
        }
    }
}