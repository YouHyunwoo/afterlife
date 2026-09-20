using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.World
{
    public class WorldMap
    {
        private readonly Vector2Int _size; // * 맵 크기
        private readonly Dictionary<WorldMapLayerType, IWorldMapLayer> _layers; // * 레이어

        #region Caching
        private IWorldMapLayer _terrainLayer;
        private IWorldMapLayer _fieldLayer;
        private TownZoneLayer _townZoneLayer;
        #endregion

        public Vector2Int Size => _size;
        public Dictionary<Vector2Int, int> TownZoneMap => _townZoneLayer.Cells;

        public WorldMap(Vector2Int size, Dictionary<WorldMapLayerType, IWorldMapLayer> layers)
        {
            _size = size;
            _layers = layers;

            // * 캐싱
            _layers.TryGetValue(WorldMapLayerType.Terrain, out _terrainLayer);
            _layers.TryGetValue(WorldMapLayerType.Field, out _fieldLayer);
            _layers.TryGetValue(WorldMapLayerType.TownZone, out var townZoneLayer);
            _townZoneLayer = townZoneLayer as TownZoneLayer;
        }

        public bool GetLayerByType<TLayer>(WorldMapLayerType layerType, out TLayer layer) where TLayer : IWorldMapLayer
        {
            layer = default;
            if (!_layers.TryGetValue(layerType, out var raw) || raw is not TLayer typed) return false;
            layer = typed;
            return true;
        }

        public void SetPassable(Vector2Int position, Vector2Int size, bool isPassable)
            => _fieldLayer.SetPassable(position, size, isPassable);

        public bool IsPassable(Vector2Int position, Vector2Int size)
            => (
                _terrainLayer.IsPassable(position, size) &&
                _fieldLayer.IsPassable(position, size)
            );

        public List<Vector2Int> GetPassablePositions(Vector2Int size)
        {
            var result = new List<Vector2Int>();
            for (var y = 0; y <= _size.y - size.y; y++)
            {
                for (var x = 0; x <= _size.x - size.x; x++)
                {
                    var position = new Vector2Int(x, y);
                    if (IsPassable(position, size))
                        result.Add(position);
                }
            }
            return result;
        }

        #region Convenience Methods
        public void PlaceField(Vector2Int position, Vector2Int size)
            => SetPassable(position, size, false);

        public void UnplaceField(Vector2Int position, Vector2Int size)
            => SetPassable(position, size, true);

        public bool IsInTownZone(Vector2Int position)
            => _townZoneLayer.IsInTownZone(position);

        public void AddTownZone(Vector3 centerPosition, float radius)
            => _townZoneLayer.AddTownZone(centerPosition, radius);

        public void RemoveTownZone(Vector3 centerPosition, float radius)
            => _townZoneLayer.RemoveTownZone(centerPosition, radius);

        public List<Vector2Int> GetTownZonePositions()
            => _townZoneLayer.GetTownZonePositions();
        #endregion
    }
}