using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class ChaseSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (cm.HasCitizenChaseTag[i] &&
                    cm.HasChaseTarget[i] &&
                    cm.HasNavMeshAgent[i] &&
                    cm.HasAttack[i] &&
                    cm.HasEntity[i])
                    UpdateChase(i, cm);
            }
        }

        private void UpdateChase(int entity, ComponentManager cm)
        {
            var targetId = cm.ChaseTargets[entity].TargetId;

            if (!cm.HasEntity[targetId] || cm.HasDiedTag[targetId])
            {
                TransitToIdle(entity, cm);
                return;
            }

            var myPosition = cm.Entities[entity].transform.position;
            var targetPosition = cm.Entities[targetId].transform.position;
            var distance = Vector3.Distance(myPosition, targetPosition);

            if (distance > cm.Attacks[entity].DetectionRange)
            {
                TransitToIdle(entity, cm);
                return;
            }

            cm.NavMeshAgents[entity].SetDestination(targetPosition);

            if (distance <= cm.Attacks[entity].AttackRange * 0.95f)
            {
                cm.HasCitizenChaseTag[entity] = false;
                cm.HasCitizenFightTag[entity] = true;
                cm.NavMeshAgents[entity].ResetPath();
            }
        }

        private void TransitToIdle(int entity, ComponentManager cm)
        {
            cm.HasCitizenChaseTag[entity] = false;
            cm.HasChaseTarget[entity] = false;
            cm.HasCitizenIdleTag[entity] = true;
        }
    }
}
