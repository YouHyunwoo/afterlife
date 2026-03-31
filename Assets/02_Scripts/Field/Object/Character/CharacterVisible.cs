using System;
using UnityEngine;
using UnityEngine.AI;

namespace Afterlife.Dev.Field
{
    public class CharacterVisible<TObject> : ObjectVisible<TObject> where TObject : Object
    {
        protected NavMeshAgent navMeshAgent;
        protected CollisionField interactionCollisionField;

        public NavMeshAgent NavMeshAgent => navMeshAgent;

        public event Action<Collider2D, CollisionField, CharacterVisible<TObject>, object> OnInteractionCollided;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            TryGetComponent(out navMeshAgent);
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;

            var interactionCollisionFieldTransform = transform.Find("Collider").Find("Interaction");
            interactionCollisionFieldTransform.TryGetComponent(out interactionCollisionField);
            interactionCollisionField.OnEnter += (collider, collisionField, sender) =>
                OnInteractionCollided?.Invoke(collider, collisionField, this, sender);
        }

        public void RefreshInteractionCollisionField()
        {
            interactionCollisionField.Collider.enabled = false;
            interactionCollisionField.Collider.enabled = true;
        }

        public void StartMovement(Vector3 destination)
            => navMeshAgent.SetDestination(destination);

        public void StopMovement()
            => navMeshAgent.SetDestination(transform.position);

        public bool HasReachedDestination()
        {
            if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
                return false;

            if (navMeshAgent.pathPending)
                return false;

            if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
                return false;

            if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < 0.01f)
                return true;

            return false;
        }
    }
}
