using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Dev.World;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class ObjectSpawnSystem : Moonstone.Ore.Local.System
    {
        private readonly Dictionary<string, ObjectVisible> _idToVisible = new();
        private WorldRepository _worldRepository;
        private World.World _world;
        private ObjectRepository _objectRepository;
        private ObjectSystem _objectSystem;

        public event Action<ObjectVisible, bool, ObjectSpawnSystem, object> OnBuilt;
        public event Action<ObjectVisible, bool, ObjectSpawnSystem, object> OnDemolished;

        protected override void OnSetUp()
        {
            _world = _worldRepository.FindAll().First();
        }

        public bool TrySpawn<TObject, TObjectVisible, TObjectData>(
            Vector2Int position, TObjectVisible prefab, TObjectData data, Func<string, TObject> factory,
            out TObject @object, out TObjectVisible visible
        ) where TObject : Object
          where TObjectVisible : ObjectVisible<TObject>
          where TObjectData : ObjectData
        {
            @object = null;
            visible = null;

            if (!CanBuild(position, data.Size)) return false;

            var objectId = Moonstone.Ore.Model.NewId();
            @object = factory(objectId);
            @object.Initialize(data);
            _objectRepository.Save(@object);

            var worldPosition = (Vector3)(Vector2)position + (Vector3)(Vector2)data.Size * 0.5f;
            visible = Instantiate(prefab, worldPosition, Quaternion.identity);
            visible.Bind(@object);

            _world.WorldMap.PlaceField(position, data.Size);

            if (@object is Building building)
                _world.WorldMap.AddTownZone(worldPosition, building.TownZoneRadius);

            _idToVisible[@object.Id] = visible;

            if (visible is CharacterVisible<Citizen> citizenVisible)
                _objectSystem.RegisterCharacter(@object, visible, citizenVisible.NavMeshAgent, citizenVisible.RefreshInteractionCollisionField);
            else if (visible is CharacterVisible<Enemy> enemyVisible)
                _objectSystem.RegisterCharacter(@object, visible, enemyVisible.NavMeshAgent, null);
            else
                _objectSystem.Register(@object, visible);

            OnBuilt?.Invoke(visible, data.Size != Vector2Int.zero, this, this);

            return true;
        }

        public void Despawn(Object @object)
        {
            if (!_idToVisible.TryGetValue(@object.Id, out var visible)) return;

            _objectSystem.Unregister(@object);

            _idToVisible.Remove(@object.Id);

            var worldPosition = visible.transform.position;

            if (@object is Building building)
                _world.WorldMap.RemoveTownZone(worldPosition, building.TownZoneRadius);

            var gridPosition = Vector2Int.FloorToInt((Vector2)worldPosition - (Vector2)@object.Size * 0.5f);
            _world.WorldMap.UnplaceField(gridPosition, @object.Size);

            Destroy(visible.gameObject);

            _objectRepository.Delete(@object.Id);

            OnDemolished?.Invoke(visible, @object.Size != Vector2Int.zero, this, this);
        }

        private bool CanBuild(Vector2Int position, Vector2Int size)
            => _world.WorldMap.IsPassable(position, size);
    }
}
