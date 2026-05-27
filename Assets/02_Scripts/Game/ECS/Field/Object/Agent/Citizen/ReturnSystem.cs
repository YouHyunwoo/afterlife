using System;
using System.Collections.Generic;
using ECS.Dev;
using UnityEngine;
using UnityEngine.AI;

namespace Afterlife.Dev.Game
{
    public class ReturnSystem : ISystem<ComponentManager>
    {
        public Func<IEnumerable<int>> GetBuildings;

        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (cm.HasCitizenReturnTag[i] &&
                    cm.HasNavMeshAgent[i] &&
                    cm.HasHolder[i])
                    UpdateReturn(i, cm);
            }
        }

        private void UpdateReturn(int entityId, ComponentManager cm)
        {
            // TODO: HarvestSystem에서 하기
            if (!cm.HasReturnTarget[entityId])
            {
                var agentPosition = cm.Entities[entityId].transform.position;
                if (FindNearest(agentPosition, BuildingType.House, cm, out var buildingId))
                {
                    var buildingEntity = cm.Entities[buildingId];
                    var destination = buildingEntity.transform.position;
                    cm.AddReturnTarget(entityId, buildingId, destination);
                    cm.MoveCommands[entityId].Destination = destination;
                    cm.MoveCommands[entityId].HasNewTarget = true;
                }
                else
                {
                    cm.ItemHolders[entityId].DropHoldings();
                    cm.Holders[entityId].HasHoldings = false;
                    cm.HasCitizenReturnTag[entityId] = false;
                    cm.HasCitizenIdleTag[entityId] = true;
                }
                return;
            }

            if (CheckArrived(cm.NavMeshAgents[entityId]))
            {
                cm.ItemHolders[entityId].DropHoldings();
                cm.Holders[entityId].HasHoldings = false;
                cm.HasReturnTarget[entityId] = false;
                cm.HasCitizenReturnTag[entityId] = false;
                cm.HasCitizenIdleTag[entityId] = true;

                cm.Player.Woods += cm.Holders[entityId].Woods;
                cm.Player.Stones += cm.Holders[entityId].Stones;
            }
        }

        private bool FindNearest(Vector3 position, BuildingType buildingType, ComponentManager cm, out int resultBuildingId)
        {
            resultBuildingId = -1;

            var buildingIds = GetBuildings?.Invoke();
            if (buildingIds == null) return false;

            var minDistance = float.MaxValue;
            foreach (var buildingId in buildingIds)
            {
                var building = cm.Buildings[buildingId];
                if (building.Type != buildingType) continue;

                var entity = cm.Entities[buildingId];
                var distance = Vector3.Distance(position, entity.transform.position);
                if (distance < minDistance)
                {
                    resultBuildingId = buildingId;
                    minDistance = distance;
                }
            }

            return resultBuildingId >= 0;
        }

        private bool CheckArrived(NavMeshAgent agent)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return false;
            if (agent.pathPending) return false;
            if (agent.remainingDistance > agent.stoppingDistance) return false;
            return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
        }
    }
}
