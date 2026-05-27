using ECS.Dev;
using UnityEngine.AI;

namespace Afterlife.Dev.Game
{
    public class ArrivalSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (cm.HasCitizenMoveTag[i] &&
                    cm.HasNavMeshAgent[i] &&
                    cm.HasEntity[i])
                    UpdateArrival(cm.NavMeshAgents[i], i, cm);
            }
        }

        private void UpdateArrival(NavMeshAgent navMeshAgent, int entityId, ComponentManager cm)
        {
            if (CheckArrived(navMeshAgent, out var isArrived) && isArrived)
            {
                cm.HasCitizenIdleTag[entityId] = true;
                cm.HasCitizenMoveTag[entityId] = false;
                cm.HasCitizenCommandedMoveTag[entityId] = false;
                cm.NavMeshAgents[entityId].ResetPath();
            }
        }

        private bool CheckArrived(NavMeshAgent navMeshAgent, out bool isArrived)
        {
            isArrived = false;
            if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return false;
            if (navMeshAgent.pathPending) return true;
            if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance) return true;
            isArrived = !navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < 0.01f;
            return true;
        }
    }
}