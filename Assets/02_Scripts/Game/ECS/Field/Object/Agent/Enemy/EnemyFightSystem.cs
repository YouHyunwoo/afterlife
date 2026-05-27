using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class EnemyFightSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasEnemyTag[i]) continue;
                if (!cm.HasEnemyFightTag[i]) continue;
                if (!cm.HasChaseTarget[i]) continue;
                if (!cm.HasAttack[i]) continue;

                UpdateEnemyFight(i, cm);
            }
        }

        private void UpdateEnemyFight(int entity, ComponentManager cm)
        {
            ref var attack = ref cm.Attacks[entity];

            if (Time.time >= attack.NextScanTime)
            {
                attack.NextScanTime = Time.time + 1f;
                var pos = cm.Entities[entity].transform.position;
                var bestId = FindPriorityTargetId(pos, entity, cm);
                if (bestId >= 0 && bestId != cm.ChaseTargets[entity].TargetId)
                    cm.ChaseTargets[entity].TargetId = bestId;
            }

            var targetId = cm.ChaseTargets[entity].TargetId;
            var position = cm.Entities[entity].transform.position;

            if (!cm.HasEntity[targetId] ||
                cm.HasDiedTag[targetId] ||
                (cm.HasHealth[targetId] && !cm.Healths[targetId].IsAlive) ||
                Vector3.Distance(position, cm.Entities[targetId].transform.position) > attack.AttackRange)
            {
                if (attack.ReturnDelayTimer < 0f)
                {
                    attack.ReturnDelayTimer = Time.time + 0.5f;
                }
                else if (Time.time >= attack.ReturnDelayTimer)
                {
                    attack.ReturnDelayTimer = -1f;
                    cm.HasChaseTarget[entity] = false;
                    cm.HasEnemyFightTag[entity] = false;
                    cm.HasEnemyMoveTag[entity] = true;
                }
                return;
            }

            attack.ReturnDelayTimer = -1f;

            if (Time.time >= attack.NextAttackTime)
            {
                attack.NextAttackTime = Time.time + attack.AttackInterval;
                cm.DamageCommands[targetId].Amount = attack.AttackPower;
                cm.DamageCommands[targetId].Attacker = cm.Entities[entity];
                cm.DamageCommands[targetId].IsPending = true;
            }
        }

        private int FindPriorityTargetId(Vector3 position, int entityId, ComponentManager cm)
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

                if (cm.HasCitizenTag[i])
                {
                    if (d < dist1st) { best1st = i; dist1st = d; }
                }
                else if (cm.HasBuildingTag[i])
                {
                    if (d < dist2nd) { best2nd = i; dist2nd = d; }
                }
            }

            return best1st >= 0 ? best1st : best2nd;
        }
    }
}
