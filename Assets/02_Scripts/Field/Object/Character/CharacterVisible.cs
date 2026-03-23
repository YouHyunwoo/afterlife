using UnityEngine;
using UnityEngine.AI;

namespace Afterlife.Dev.Field
{
    public class CharacterVisible : ObjectVisible
    {
        protected NavMeshAgent navMeshAgent;
        protected CollisionField interactionCollisionField;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            TryGetComponent(out navMeshAgent);
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;

            var interactionCollisionFieldTransform = transform.Find("Collider").Find("Interaction");
            interactionCollisionFieldTransform.TryGetComponent(out interactionCollisionField);
        }

        public void StartMovement(Vector3 destination)
        {
            navMeshAgent.SetDestination(destination);
        }

        public void StopMovement()
        {
            navMeshAgent.SetDestination(transform.position);
        }

        public bool HasReachedDestination()
        {
            if (navMeshAgent == null || !navMeshAgent.enabled)
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