using ECS.Dev;
using UnityEngine.AI;

namespace Afterlife.Dev.Game
{
    public class BuildSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasCitizenTag[i]) continue;
                if (!cm.HasBuildTarget[i]) continue;
                if (!cm.HasHolder[i]) continue;
                if (!cm.HasCitizenBuildTag[i]) continue;
                if (!cm.HasNavMeshAgent[i]) continue;

                UpdateBuild(i, cm);
            }
        }

        private void UpdateBuild(int entityId, ComponentManager cm)
        {
            ref var buildTarget = ref cm.BuildTargets[entityId];

            if (buildTarget.BuildingEntityId < 0)
            {
                TransitToIdle(entityId, cm);
                return;
            }

            var buildingEntityId = buildTarget.BuildingEntityId;
            ref var building = ref cm.Buildings[buildingEntityId];

            if (building.IsBuilt)
            {
                if (buildTarget.IsBuilding)
                {
                    building.BuildSpeed -= 0.2f;
                    building.WorkerCount--;
                    buildTarget.IsBuilding = false;
                }
                cm.HasBuildTarget[entityId] = false;
                cm.HasCitizenBuildTag[entityId] = false;
                cm.HasCitizenIdleTag[entityId] = true;
                return;
            }

            if (!buildTarget.IsBuilding && CheckArrived(cm.NavMeshAgents[entityId]))
            {
                cm.NavMeshAgents[entityId].ResetPath();
                building.BuildSpeed += 0.2f;
                building.WorkerCount++;
                buildTarget.IsBuilding = true;
            }
        }

        private bool CheckArrived(NavMeshAgent agent)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return false;
            if (agent.pathPending) return false;
            if (agent.remainingDistance > agent.stoppingDistance) return false;
            return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
        }

        private void TransitToIdle(int entityId, ComponentManager cm)
        {
            ref var buildTarget = ref cm.BuildTargets[entityId];
            if (buildTarget.IsBuilding && cm.HasBuildingTag[buildTarget.BuildingEntityId])
            {
                cm.Buildings[buildTarget.BuildingEntityId].BuildSpeed -= 0.2f;
                cm.Buildings[buildTarget.BuildingEntityId].WorkerCount--;
            }
            cm.HasBuildTarget[entityId] = false;
            cm.HasCitizenBuildTag[entityId] = false;
            cm.HasCitizenIdleTag[entityId] = true;
        }
    }
}
