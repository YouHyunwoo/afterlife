using Afterlife.Dev.Town;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class ConstructionSystem : Moonstone.Ore.Local.System
    {
        [SerializeField] private TownAreaSystem _townAreaSystem;

        public TObjectVisible Build<TObjectVisible, TObjectData>(Vector2Int position, TObjectVisible objectVisiblePrefab, TObjectData objectData)
            where TObjectVisible : ObjectVisible where TObjectData : ObjectData
        {
            // TODO: 위치에 건설 가능한지 체크 (충돌, 타일맵 정보 등등)
            var worldPosition = (Vector3)(Vector2)position + (Vector3)(Vector2)objectData.Size * 0.5f;
            var objectVisible = Instantiate(objectVisiblePrefab, worldPosition, Quaternion.identity);
            objectVisible.SetData(objectData);

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

            Destroy(objectVisible.gameObject);
        }
    }
}