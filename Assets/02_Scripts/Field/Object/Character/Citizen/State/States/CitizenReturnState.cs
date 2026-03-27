using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenReturnState : CitizenState
    {
        private HouseVisible _nearestHouseVisible;

        public CitizenReturnState(string stateId) : base(stateId)
        {
        }

        protected override void OnEnter(object[] args)
        {
            if (FindNearestHouseVisible(out _nearestHouseVisible))
            {
                visible.OnInteractionCollided += HandleInteractionCollided;
                visible.StartMovement(_nearestHouseVisible.transform.position);
            }
            else
            {
                visible.ClearHoldableVisibles();
                Transit("idle");
            }
        }

        private void HandleInteractionCollided(Collider2D collider, CollisionField collisionField, CharacterVisible characterVisible, object sender)
        {
            if (_nearestHouseVisible.transform == collider.transform)
            {
                visible.StopMovement();
                visible.ObtainHoldables();
                Transit("idle");
            }
        }

        private bool FindNearestHouseVisible(out HouseVisible nearestHouseVisible)
        {
            nearestHouseVisible = null;

            var objectMap = context.BuildSystem.ObjectMap;
            if (objectMap.ContainsKey("HouseVisible"))
            {
                var position = visible.transform.position;
                var minHouseVisible = (HouseVisible)null;
                var minDistance = float.MaxValue;
                foreach (var objectVisible in objectMap["HouseVisible"])
                {
                    if (objectVisible is not HouseVisible houseVisible) continue;
                    var targetPosition = houseVisible.transform.position;
                    var distance = Vector3.Distance(position, targetPosition);
                    if (minDistance > distance)
                    {
                        minHouseVisible = houseVisible;
                        minDistance = distance;
                    }
                }
                nearestHouseVisible = minHouseVisible;
            }

            return true;
        }

        protected override void OnExit(object[] args)
        {
            visible.OnInteractionCollided -= HandleInteractionCollided;
            _nearestHouseVisible = null;
        }
    }
}