using System;
using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public record SpawnResultInfo(int EntityId, bool HasVolume);
    public record DespawnResultInfo(int EntityId, bool HasVolume);

    public partial class EntityFactory
    {
        private EntityManager _entityManager;
        private ComponentManager _componentManager;

        public event Action<SpawnResultInfo, EntityFactory> OnSpawned;
        public event Action<DespawnResultInfo, EntityFactory> OnDespawned;

        public bool TrySpawn(Vector3 position, ObjectData data, out Entity entity)
        {
            var entityId = _entityManager.Create();
            var gameObject = UnityEngine.Object.Instantiate(data.Prefab, position, Quaternion.identity);

            if (!gameObject.TryGetComponent(out entity))
                entity = gameObject.AddComponent<Entity>();
            entity.Id = entityId;
            entity.ComponentManager = _componentManager;
            RouteSpawn(position, data, entity);
            
            OnSpawned?.Invoke(new(entityId, data.Volume2DSize != Vector2Int.zero), this);

            return true;
        }

        public void Despawn(int entityId)
        {
            var hasVolume = (
                _componentManager.HasVolume[entityId] &&
                _componentManager.Volumes[entityId].Size != Vector2Int.zero
            );
            OnDespawned?.Invoke(new(entityId, hasVolume), this);

            UnityEngine.Object.Destroy(_componentManager.Entities[entityId].gameObject);
            _entityManager.Destroy(entityId);
        }

        private bool RouteSpawn(Vector3 position, ObjectData data, Entity entity)
        {
            if (data is CitizenData citizenData)
            {
                CreateCitizen(citizenData, entity);
            }
            else if (data is EnemyData enemyData)
            {
                CreateEnemy(enemyData, entity);
            }
            else if (data is BuildingData buildingData)
            {
                CreateBuilding(buildingData, entity);
            }
            else if (data is ResourceData resourceData)
            {
                CreateResource(resourceData, entity);
            }
            else
                return false;
            
            return true;
        }
    }
}
