using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class EnemyMoveSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasEntity[i]) continue;
                if (!cm.HasEnemyTag[i]) continue;
                if (!cm.HasEnemyMoveTag[i]) continue;
                if (!cm.HasNavMeshAgent[i]) continue;
                if (!cm.HasAttack[i]) continue;

                UpdateEnemyMove(i, cm);
            }
        }

        private void UpdateEnemyMove(int entityId, ComponentManager cm)
        {
            ref var attack = ref cm.Attacks[entityId];
            var position = cm.Entities[entityId].transform.position;

            if (Time.time >= attack.NextScanTime)
            {
                attack.NextScanTime = Time.time + 0.5f;
                var nearbyId = FindTarget(position, entityId, cm, attack.DetectionRange);
                if (nearbyId >= 0)
                {
                    cm.AddChaseTarget(entityId, nearbyId);
                }
                else
                {
                    var globalId = FindTarget(position, entityId, cm, float.MaxValue);
                    if (globalId >= 0)
                        cm.AddChaseTarget(entityId, globalId);
                    else
                        cm.HasChaseTarget[entityId] = false;
                }
            }

            if (!cm.HasChaseTarget[entityId]) return;

            var targetId = cm.ChaseTargets[entityId].TargetId;
            if (!IsTargetValid(targetId, cm))
            {
                attack.NextScanTime = 0f;
                return;
            }

            var targetPosition = cm.Entities[targetId].transform.position;
            var distance = Vector3.Distance(position, targetPosition);

            if (distance <= attack.AttackRange * 0.95f)
            {
                cm.NavMeshAgents[entityId].ResetPath();
                attack.NextAttackTime = Time.time + attack.AttackInterval;
                cm.HasEnemyMoveTag[entityId] = false;
                cm.HasEnemyFightTag[entityId] = true;
                return;
            }

            var agent = cm.NavMeshAgents[entityId];
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

            agent.SetDestination(targetPosition);

            if (!agent.pathPending && agent.remainingDistance < 0.3f)
                attack.NextScanTime = 0f;
        }

        private bool IsTargetValid(int targetId, ComponentManager cm)
        {
            if (!cm.HasEntity[targetId]) return false;
            if (cm.HasDiedTag[targetId]) return false;
            if (cm.HasHealth[targetId] && !cm.Healths[targetId].IsAlive) return false;
            return true;
        }

        private int FindTarget(Vector3 position, int entityId, ComponentManager cm, float maxRange)
        {
            var best1st = -1;
            var best2nd = -1;
            var dist1st = float.MaxValue;
            var dist2nd = float.MaxValue;

            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (i == entityId) continue;
                if (!cm.HasEntity[i]) continue;
                if (cm.HasDiedTag[i]) continue;
                if (cm.HasHealth[i] && !cm.Healths[i].IsAlive) continue;

                var d = Vector3.Distance(position, cm.Entities[i].transform.position);
                if (d > maxRange) continue;

                if (cm.HasCitizenTag[i])
                {
                    if (d < dist1st) { best1st = i; dist1st = d; }
                }
                else if (cm.HasBuildingTag[i] || cm.HasHouseTag[i])
                {
                    if (d < dist2nd) { best2nd = i; dist2nd = d; }
                }
            }

            return best1st >= 0 ? best1st : best2nd;
        }
    }
}
