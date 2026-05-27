using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class DetectionSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasEntity[i]) continue;
                if (!cm.HasCitizenTag[i]) continue;
                if (!cm.HasAttack[i]) continue;
                if (!cm.HasCitizenIdleTag[i] && !cm.HasCitizenMoveTag[i]) continue;
                if (cm.HasCitizenMoveTag[i] && cm.HasCitizenCommandedMoveTag[i]) continue;

                UpdateDetection(i, cm);
            }
        }

        private void UpdateDetection(int entityId, ComponentManager cm)
        {
            ref var attack = ref cm.Attacks[entityId];

            if (Time.time < attack.NextScanTime) return;
            attack.NextScanTime = Time.time + 0.5f;

            var position = cm.Entities[entityId].transform.position;
            var nearestEnemyId = FindNearestEnemy(position, cm, attack.DetectionRange);

            if (nearestEnemyId < 0) return;

            cm.HasCitizenIdleTag[entityId] = false;
            cm.HasCitizenMoveTag[entityId] = false;
            cm.HasCitizenChaseTag[entityId] = true;
            cm.AddChaseTarget(entityId, nearestEnemyId);

            if (cm.HasNavMeshAgent[entityId])
                cm.NavMeshAgents[entityId].SetDestination(cm.Entities[nearestEnemyId].transform.position);
        }

        private int FindNearestEnemy(Vector3 position, ComponentManager cm, float maxRange)
        {
            var bestId = -1;
            var bestDist = float.MaxValue;

            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasEntity[i]) continue;
                if (!cm.HasEnemyTag[i]) continue;
                if (cm.HasDiedTag[i]) continue;
                if (cm.HasHealth[i] && !cm.Healths[i].IsAlive) continue;

                var d = Vector3.Distance(position, cm.Entities[i].transform.position);
                if (d > maxRange) continue;
                if (d < bestDist) { bestId = i; bestDist = d; }
            }

            return bestId;
        }
    }
}
