using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public partial class EntityFactory
    {
        public void CreateResource(ResourceData data, Entity entity)
        {
            var entityId = entity.Id;

            // Object
            _componentManager.AddEntity(entityId, entity);
            _componentManager.HasDespawnTag[entityId] = false;
            _componentManager.AddHealth(entityId, 10f);
            _componentManager.AddDamageCommand(entityId);
            _componentManager.HasDiedTag[entityId] = false;
            _componentManager.AddSelectCommand(entityId);
            _componentManager.HasSelectedTag[entityId] = false;
            _componentManager.AddLight(entityId, data.Light);
            _componentManager.AddFogAlpha(entityId, entity.GetComponentsInChildren<SpriteRenderer>());

            // Placeable
            _componentManager.AddVolume(entityId, data.Volume2DSize);
            if (entity.TryGetComponent(out Volume2DSynchronizer volume2DSynchronizer))
                volume2DSynchronizer.Sync();

            // Resource
            _componentManager.AddResource(entityId, data.Woods, data.Stones, data.HarvestSpeed);

        }
    }
}
