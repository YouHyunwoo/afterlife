using System.Collections.Generic;
using ECS.Dev;
using UnityEngine.AI;

namespace Afterlife.Dev.Game
{
    public class HarvestSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (cm.HasCitizenHarvestTag[i] &&
                    cm.HasHarvestTarget[i] &&
                    cm.HasNavMeshAgent[i] &&
                    cm.HasHolder[i])
                    UpdateHarvest(i, deltaTime, cm);
            }
        }

        private void UpdateHarvest(int entityId, float deltaTime, ComponentManager cm)
        {
            ref var harvestTarget = ref cm.HarvestTargets[entityId];

            // * 채취 대상이 없으면 Idle 상태로
            if (harvestTarget.ResourceEntityId < 0)
            {
                TransitToIdle(entityId, cm);
                return;
            }

            ref var resource = ref cm.Resources[harvestTarget.ResourceEntityId];

            if (!harvestTarget.IsHarvesting) // * 다가가는 중...
            {
                // * 접촉하면
                if (CheckArrived(cm.NavMeshAgents[entityId])) // TODO: 자원과 충돌하면 도착하도록 수정 -> hasPath로 검사될듯
                {
                    // * 채취된 자원이면 Idle 상태로
                    if (resource.IsHarvested)
                    {
                        TransitToIdle(entityId, cm);
                        return;
                    }

                    // * 시민 정지 및 채취 시작
                    cm.NavMeshAgents[entityId].ResetPath();
                    resource.HarvestSpeed += 0.2f;
                    resource.WorkerCount++;
                    harvestTarget.IsHarvesting = true;
                }
                // TODO: 접촉하지 않으면 계속 다가가려고 시도 -> 오래 못하면 Idle로 전환
            }
            else // * 채취 중...
            {
                // * 채취가 완료되면
                if (resource.IsHarvested)
                {
                    TransitToIdle(entityId, cm);
                }
                // * 채취 중이면
                else
                {
                    // * 채취 처리
                    resource.HarvestRate += deltaTime * resource.HarvestSpeed;
                    if (resource.HarvestRate >= 1f)
                        FinishHarvest(entityId, cm);
                }
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
            if (cm.HasHarvestTarget[entityId])
            {
                ref var harvestTarget = ref cm.HarvestTargets[entityId];
                if (harvestTarget.ResourceEntityId >= 0 && harvestTarget.IsHarvesting)
                {
                    ref var resource = ref cm.Resources[harvestTarget.ResourceEntityId];
                    resource.WorkerCount--;
                    resource.HarvestSpeed -= 0.2f; // TODO: 하드코딩 되어 있음 -> 워커 능력치로 전환
                }
                cm.HasHarvestTarget[entityId] = false;
            }

            // * Idle 상태로 전환
            cm.HasCitizenHarvestTag[entityId] = false;
            cm.HasCitizenIdleTag[entityId] = true;
        }

        private void FinishHarvest(int entityId, ComponentManager cm)
        {
            ref var harvestTarget = ref cm.HarvestTargets[entityId];
            var resourceEntityId = harvestTarget.ResourceEntityId;

            ref var resource = ref cm.Resources[resourceEntityId];
            resource.HarvestRate = 1f;
            resource.HarvestSpeed -= resource.WorkerCount * 0.2f;
            resource.WorkerCount = 0;
            resource.State = HarvestState.Harvested;

            var workerIds = new List<int>();
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasHarvestTarget[i]) continue;
                var ht = cm.HarvestTargets[i];
                if (ht.ResourceEntityId == harvestTarget.ResourceEntityId &&
                    cm.HasHolder[i])
                    workerIds.Add(i);
            }

            var count = workerIds.Count;
            if (count > 0)
            {
                var woods = resource.Woods;
                var stones = resource.Stones;
                int woodsBase = woods / count, woodsRem = woods % count;
                int stonesBase = stones / count, stonesRem = stones % count;
                for (int i = 0; i < count; i++)
                {
                    int w = woodsBase + (i < woodsRem ? 1 : 0);
                    int s = stonesBase + (i < stonesRem ? 1 : 0);

                    var workerEntityId = workerIds[i];
                    // * workerIds[i], w, s
                    cm.Holders[workerEntityId].Woods = w;
                    cm.Holders[workerEntityId].Stones = s;
                    cm.Holders[workerEntityId].HasHoldings = true;
                    cm.ItemHolders[workerEntityId].TakeHoldings(w, s);

                    cm.HasHarvestTarget[workerEntityId] = false;

                    cm.HasCitizenHarvestTag[workerEntityId] = false;
                    cm.HasCitizenReturnTag[workerEntityId] = true;
                }
            }

            cm.HasResourceTag[resourceEntityId] = false;
            cm.HasDespawnTag[resourceEntityId] = true;
        }
    }
}
