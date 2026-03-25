using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class BuildSystem : Moonstone.Ore.Local.System
    {
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private NavigationSystem _navigationSystem;

        private readonly Dictionary<string, List<ObjectVisible>> _objectMap = new();

        public event Action<ObjectVisible, BuildSystem, object> OnBuilt;
        public event Action<ObjectVisible, BuildSystem, object> OnDemolished;

        public Dictionary<string, List<ObjectVisible>> ObjectMap => _objectMap;

        public bool TryBuild<TObjectVisible, TObjectData>(
            Vector2Int position, TObjectVisible objectVisiblePrefab, TObjectData objectData, out TObjectVisible objectVisible
        ) where TObjectVisible : ObjectVisible where TObjectData : ObjectData
        {
            objectVisible = null;

            if (!CanBuild(position, objectData.Size)) return false;

            var worldPosition = (Vector3)(Vector2)position + (Vector3)(Vector2)objectData.Size * 0.5f;
            objectVisible = Instantiate(objectVisiblePrefab, worldPosition, Quaternion.identity);
            objectVisible.SetData(objectData);

            _gridSystem.PlaceField(position, objectData.Size);

            // 건물이 TownAreaSystem에 영향을 주는 경우, 영향 추가
            if (objectVisible is BuildingVisible buildingVisible)
                _townAreaSystem.AddInfluence(worldPosition, buildingVisible.TownAreaInfluenceRadius);

            RegisterObject(objectVisible.GetType().Name, objectVisible);

            OnBuilt?.Invoke(objectVisible, this, this);

            if (objectVisible.Size != Vector2.zero)
                _navigationSystem.BuildNavMesh();

            return true;
        }

        private bool CanBuild(Vector2Int position, Vector2Int size)
            => (
                _gridSystem.IsPassable(GridLayer.Terrain, position, size) &&
                _gridSystem.IsPassable(GridLayer.Field, position, size)
            );

        private void RegisterObject(string type, ObjectVisible objectVisible)
        {
            if (!_objectMap.ContainsKey(type))
                _objectMap.Add(type, new());

            _objectMap[type].Add(objectVisible);
        }

        public void Demolish<TObjectVisible>(
            TObjectVisible objectVisible
        ) where TObjectVisible : ObjectVisible
        {
            OnDemolished?.Invoke(objectVisible, this, this);

            DeregisterObject(objectVisible.GetType().Name, objectVisible);

            var worldPosition = objectVisible.transform.position;

            // 건물이 TownAreaSystem에 영향을 주는 경우, 영향 제거
            if (objectVisible is BuildingVisible buildingVisible)
                _townAreaSystem.RemoveInfluence(worldPosition, buildingVisible.TownAreaInfluenceRadius);

            var position = Vector2Int.FloorToInt((Vector2)worldPosition - (Vector2)objectVisible.Size * 0.5f);
            _gridSystem.UnplaceField(position, objectVisible.Size);

            Destroy(objectVisible.gameObject);

            if (objectVisible.Size != Vector2.zero)
                _navigationSystem.BuildNavMesh();
        }

        private void DeregisterObject(string type, ObjectVisible objectVisible)
        {
            if (!_objectMap.ContainsKey(type)) return;

            _objectMap[type].Remove(objectVisible);
            if (_objectMap[type].Count <= 0)
                _objectMap.Remove(type);
        }
    }
}