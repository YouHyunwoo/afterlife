using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class FightSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (cm.HasCitizenFightTag[i] &&
                    cm.HasChaseTarget[i] &&
                    cm.HasAttack[i] &&
                    cm.HasEntity[i])
                    UpdateFight(i, cm);
            }
        }

        private void UpdateFight(int entity, ComponentManager cm)
        {
            var targetId = cm.ChaseTargets[entity].TargetId;

            if (!cm.HasEntity[targetId] || cm.HasDiedTag[targetId] ||
                (cm.HasHealth[targetId] && !cm.Healths[targetId].IsAlive))
            {
                cm.HasCitizenFightTag[entity] = false;
                cm.HasChaseTarget[entity] = false;
                cm.HasCitizenIdleTag[entity] = true;
                return;
            }

            var myPosition = cm.Entities[entity].transform.position;
            var targetPosition = cm.Entities[targetId].transform.position;
            var distance = Vector3.Distance(myPosition, targetPosition);
            ref var attack = ref cm.Attacks[entity];

            if (distance > attack.AttackRange)
            {
                cm.HasCitizenFightTag[entity] = false;
                cm.HasCitizenChaseTag[entity] = true;
                return;
            }

            if (Time.time >= attack.NextAttackTime)
            {
                attack.NextAttackTime = Time.time + attack.AttackInterval;
                cm.DamageCommands[targetId].Amount = attack.AttackPower;
                cm.DamageCommands[targetId].Attacker = cm.Entities[entity];
                cm.DamageCommands[targetId].IsPending = true;
            }
        }
    }
}
