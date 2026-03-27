using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenHarvestState : CitizenState
    {
        private ResourceVisible _targetResourceVisible;
        private bool _isAttached;

        public CitizenHarvestState(string stateId) : base(stateId)
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

                _targetResourceVisible = (ResourceVisible)args[0];
                visible.StartMovement(_targetResourceVisible.transform.position);
            }
        }

        protected override void OnExit(object[] args)
        {
            if (_isAttached)
            {
                _targetResourceVisible.DetachCitizen(visible);
                _isAttached = false;
            }
            _targetResourceVisible.OnHarvested -= HandleHarvested;
            visible.OnInteractionCollided -= HandleInteractionCollided;
        }

        private void HandleInteractionCollided(Collider2D collider, CollisionField collisionField, CharacterVisible characterVisible, object sender)
        {
            if (_targetResourceVisible.transform == collider.transform)
            {
                if (!_targetResourceVisible.IsHarvested)
                {
                    visible.StopMovement();
                    _targetResourceVisible.OnHarvested += HandleHarvested;
                    _isAttached = true;
                    _targetResourceVisible.AttachCitizen(visible);
                }
            }
        }

        private void HandleHarvested(HoldableVisible holdableVisible, ResourceVisible resourceVisible, object sender)
        {
            visible.AddHoldable(holdableVisible);
            Transit("return");
        }
    }
}