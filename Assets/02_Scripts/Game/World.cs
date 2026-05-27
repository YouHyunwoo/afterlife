using System.Collections.Generic;
using Afterlife.Dev.World;
using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class World : Moonstone.Ore.Local.Entity
    {
        [SerializeField] private Terrain _terrain;
        [SerializeField] private Field _field;
        [SerializeField] private TownZone _townZone;
        [SerializeField] private Fog _fog;

        private EntityFactory _entityFactory;
        private Vector2Int _size;
        private Dev.World.TerrainLayer _terrainLayer;
        private FieldLayer _fieldLayer;
        private TownZoneLayer _townZoneLayer;
        private FogLayer _fogLayer;
        private readonly Dictionary<int, (Entity, ObjectData)> _entities = new();

        public Vector2Int Size => _size;
        public Dev.World.TerrainLayer TerrainLayer => _terrainLayer;
        public FieldLayer FieldLayer => _fieldLayer;
        public TownZoneLayer TownZoneLayer => _townZoneLayer;
        public FogLayer FogLayer => _fogLayer;
        public Fog Fog => _fog;

        private void OnDrawGizmos()
        {
            var worldMapSize = _size;

            if (_fieldLayer != null)
            {
                Gizmos.color = new Color(1, 0, 0, 0.5f);
                for (var y = 0; y < worldMapSize.y; y++)
                {
                    for (var x = 0; x < worldMapSize.x; x++)
                    {
                        var position = new Vector2(x + 0.5f, y + 0.5f);
                        if (!_fieldLayer.IsPassable(new Vector2Int(x, y), Vector2Int.one))
                            Gizmos.DrawCube(position, Vector3.one * 0.5f);
                    }
                }
            }

            if (_townZoneLayer.Cells.Count > 0)
            {
                var offset = new Vector3(0.5f, 0.5f);
                Gizmos.color = new Color(1, 0, 1, 0.3f);
                foreach (var cell in _townZoneLayer.Cells.Keys)
                {
                    var position = (Vector3)(Vector2)cell + offset;
                    Gizmos.DrawCube(position, Vector3Int.one);
                }
            }
        }

        public GenerationResult Generate(WorldMapGenerationParameter @params)
        {
            var generator = new WorldMapGenerator();
            var isGenerated = generator.Generate(@params, out var generationResult);
            if (isGenerated)
            {
                _size = @params.MapSize;
                _terrainLayer = generationResult.TerrainLayer;
                _fieldLayer = generationResult.FieldLayer;
                _townZoneLayer = generationResult.TownZoneLayer;
                _fogLayer = new FogLayer(_size);
                _terrain.Render(_terrainLayer);
                _fog.Render(_size);
            }
            return generationResult;
        }

        #region Terrain
        public void BuildNavMesh()
            => _terrain.BuildNavMesh();
        #endregion

        #region Field
        public bool TrySpawn(Vector2Int position, ObjectData data, out Entity entity)
        {
            entity = null;

            var size = data.Volume2DSize;
            if (!IsPassable(position, size)) return false;

            var worldPosition = (Vector3)((Vector2)position + (Vector2)data.Volume2DSize * 0.5f);
            if (!_entityFactory.TrySpawn(worldPosition, data, out entity))
                return false;

            var entityId = entity.Id;

            _entities[entityId] = (entity, data);

            PlaceField(position, size);
            if (data is BuildingData buildingData)
                AddTownZone(worldPosition, buildingData.TownZoneRadius);

            return true;
        }

        public void Despawn(int entityId)
        {
            if (!_entities.TryGetValue(entityId, out var entityPair)) return;
            var (entity, data) = entityPair;

            var worldPosition = gameObject.transform.position;
            var size = data.Volume2DSize;

            if (data is BuildingData buildingData)
                RemoveTownZone(worldPosition, buildingData.TownZoneRadius);

            var position = Vector2Int.FloorToInt((Vector2)worldPosition - (Vector2)size * 0.5f);
            UnplaceField(position, size);

            _entityFactory.Despawn(entityId);
        }
        #endregion

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
