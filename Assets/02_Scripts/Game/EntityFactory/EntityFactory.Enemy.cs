using ECS.Dev;
using UnityEngine;
using UnityEngine.AI;

namespace Afterlife.Dev.Game
{
    public partial class EntityFactory
    {
        public void CreateEnemy(EnemyData data, Entity entity)
        {
            var entityId = entity.Id;

            // Object
            _componentManager.AddEntity(entityId, entity);
            _componentManager.HasDespawnTag[entityId] = false;
            _componentManager.AddHealth(entityId, data.Health);
            _componentManager.AddDamageCommand(entityId);
            _componentManager.HasDiedTag[entityId] = false;
            _componentManager.AddSelectCommand(entityId);
            _componentManager.HasSelectedTag[entityId] = false;
            _componentManager.AddLight(entityId, data.Light);
            _componentManager.AddFogAlpha(entityId, entity.GetComponentsInChildren<SpriteRenderer>());

            // Agent
            if (entity.TryGetComponent(out NavMeshAgent navMeshAgent))
            {
                navMeshAgent.updateRotation = false;
                navMeshAgent.updateUpAxis = false;
                navMeshAgent.speed = data.MoveSpeed;
                _componentManager.AddNavMeshAgent(entityId, navMeshAgent);
            }
            _componentManager.AddMoveCommand(entityId, default, false);
            _componentManager.AddAttack(entityId, data.AttackPower, data.AttackRange, data.AttackInterval, data.DetectionRange);
            if (entity.TryGetComponent(out ItemHolder itemHolder))
                _componentManager.AddHolder(entityId, itemHolder);

            // Enemy
            _componentManager.HasEnemyTag[entityId] = true;
            _componentManager.HasEnemyMoveTag[entityId] = true;
            _componentManager.HasEnemyFightTag[entityId] = false;

        }
    }
}
