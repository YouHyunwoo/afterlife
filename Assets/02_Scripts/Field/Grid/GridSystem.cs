using System.Collections.Generic;
using Afterlife.Dev.World;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class GridSystem : Moonstone.Ore.Local.System
    {
        private WorldSystem _worldSystem;
        private World.World _world;
        private World.TerrainLayer _terrainLayer;
        private World.FieldLayer _fieldLayer;

        private void OnDrawGizmos()
        {
            if (_world == null) return;

            var worldMapSize = _world.WorldMap.Size;

            // if (_terrainLayer != null)
            // {
            //     Gizmos.color = new Color(0, 1, 0, 0.5f);
            //     for (var y = 0; y < worldMapSize.y; y++)
            //     {
            //         for (var x = 0; x < worldMapSize.x; x++)
            //         {
            //             var position = new Vector2(x + 0.5f, y + 0.5f);
            //             Gizmos.DrawWireCube(position, Vector3.one);
            //             if (!_terrainLayer.IsPassable(new Vector2Int(x, y), Vector2Int.one))
            //                 Gizmos.DrawCube(position, Vector3.one * 0.8f);
            //         }
            //     }
            // }

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
        }

        protected override void OnSetUp()
        {
            _world = _worldSystem.World;

            if (!_world.WorldMap.GetLayerByType<World.TerrainLayer>(WorldMapLayerType.Terrain, out var terrainLayer)) return;
            if (!_world.WorldMap.GetLayerByType<FieldLayer>(WorldMapLayerType.Field, out var fieldLayer)) return;

            _terrainLayer = terrainLayer;
            _fieldLayer = fieldLayer;
        }

        public void SetPassable(Vector2Int position, Vector2Int size, bool isPassable)
        {
            if (_fieldLayer == null) return;
            _fieldLayer.SetPassable(position, size, isPassable);
        }

        public bool IsPassable(Vector2Int position, Vector2Int size)
        {
            return (
                _terrainLayer.IsPassable(position, size) &&
                _fieldLayer.IsPassable(position, size)
            );
        }

        public void PlaceField(Vector2Int position, Vector2Int size)
            => SetPassable(position, size, false);

        public void UnplaceField(Vector2Int position, Vector2Int size)
            => SetPassable(position, size, true);

        public List<Vector2Int> GetPassablePositions(Vector2Int size)
        {
            var result = new List<Vector2Int>();
            var mapSize = _world.WorldMap.Size;
            for (var y = 0; y <= mapSize.y - size.y; y++)
            {
                for (var x = 0; x <= mapSize.x - size.x; x++)
                {
                    var position = new Vector2Int(x, y);
                    if (IsPassable(position, size))
                        result.Add(position);
                }
            }
            return result;
        }
    }
}