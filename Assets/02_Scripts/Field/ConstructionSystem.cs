using Afterlife.Dev.Town;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class ConstructionSystem : Moonstone.Ore.Local.System
    {
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private TownAreaSystem _townAreaSystem;

        public bool TryBuild<TObjectVisible, TObjectData>(
            Vector2Int position, TObjectVisible objectVisiblePrefab, TObjectData objectData, out TObjectVisible objectVisible
        ) where TObjectVisible : ObjectVisible where TObjectData : ObjectData
        {
            objectVisible = null;

            var isPassable = _gridSystem.IsPassable(GridLayer.Terrain, position, objectData.Size) &&
                _gridSystem.IsPassable(GridLayer.Field, position, objectData.Size);
            if (!isPassable)
            {
                return false;
            }

            var worldPosition = (Vector3)(Vector2)position + (Vector3)(Vector2)objectData.Size * 0.5f;
            objectVisible = Instantiate(objectVisiblePrefab, worldPosition, Quaternion.identity);
            objectVisible.SetData(objectData);

            _gridSystem.PlaceField(position, objectData.Size);

            // 건물이 TownAreaSystem에 영향을 주는 경우, 영향 추가
            if (objectVisible is BuildingVisible buildingVisible)
                _townAreaSystem.AddInfluence(worldPosition, buildingVisible.TownAreaInfluenceRadius);

            return true;
        }

        public TObjectVisible Build<TObjectVisible, TObjectData>(Vector2Int position, TObjectVisible objectVisiblePrefab, TObjectData objectData)
            where TObjectVisible : ObjectVisible where TObjectData : ObjectData
        {
            var isPassable = _gridSystem.IsPassable(GridLayer.Terrain, position, objectData.Size) &&
                _gridSystem.IsPassable(GridLayer.Field, position, objectData.Size);
            if (!isPassable)
            {
                return null;
            }

            var worldPosition = (Vector3)(Vector2)position + (Vector3)(Vector2)objectData.Size * 0.5f;
            var objectVisible = Instantiate(objectVisiblePrefab, worldPosition, Quaternion.identity);
            objectVisible.SetData(objectData);

            _gridSystem.PlaceField(position, objectData.Size);

            // 건물이 TownAreaSystem에 영향을 주는 경우, 영향 추가
            if (objectVisible is BuildingVisible buildingVisible)
                _townAreaSystem.AddInfluence(worldPosition, buildingVisible.TownAreaInfluenceRadius);

            return objectVisible;
        }

        public void Demolish(ObjectVisible objectVisible)
        {
            // 건물이 TownAreaSystem에 영향을 주는 경우, 영향 제거
            if (objectVisible is BuildingVisible buildingVisible)
                _townAreaSystem.RemoveInfluence(objectVisible.transform.position, buildingVisible.TownAreaInfluenceRadius);

            var position = Vector2Int.FloorToInt((Vector2)objectVisible.transform.position);
            _gridSystem.UnplaceField(position, objectVisible.Size);

            Destroy(objectVisible.gameObject);
        }
    }
}