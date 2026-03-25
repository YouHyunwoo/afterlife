using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.Dev.Field
{
    public class GridSystem : Moonstone.Ore.Local.System
    {
        [SerializeField] private Tilemap[] _tilemaps;

        private readonly Dictionary<GridLayer, int[,]> _grids = new();
        private Vector2Int _gridSize;

        private void OnDrawGizmos()
        {
            if (_grids.ContainsKey(GridLayer.Terrain) && _grids[GridLayer.Terrain] != null)
            {
                Gizmos.color = Color.green;
                for (var y = 0; y < _gridSize.y; y++)
                {
                    for (var x = 0; x < _gridSize.x; x++)
                    {
                        var position = new Vector2(x + 0.5f, y + 0.5f);
                        Gizmos.DrawWireCube(position, Vector3.one);
                        if (_grids[GridLayer.Terrain][x, y] > 0)
                            Gizmos.DrawCube(position, Vector3.one * 0.8f);
                    }
                }
            }

            if (_grids.ContainsKey(GridLayer.Field) && _grids[GridLayer.Field] != null)
            {
                Gizmos.color = Color.red;
                for (var y = 0; y < _gridSize.y; y++)
                {
                    for (var x = 0; x < _gridSize.x; x++)
                    {
                        var position = new Vector2(x + 0.5f, y + 0.5f);
                        if (_grids[GridLayer.Field][x, y] > 0)
                            Gizmos.DrawCube(position, Vector3.one * 0.5f);
                    }
                }
            }
        }

        protected override void OnSetUp()
        {
            _grids[GridLayer.Terrain] = new int[_gridSize.x, _gridSize.y];
            _grids[GridLayer.Field] = new int[_gridSize.x, _gridSize.y];

            FillTerrainPassableFromTilemap();
        }

        private void FillTerrainPassableFromTilemap()
        {
            foreach (var tilemap in _tilemaps)
            {
                if (tilemap == null) continue;
                var b = tilemap.cellBounds;
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    for (int x = b.xMin; x < b.xMax; x++)
                    {
                        var pos = new Vector3Int(x, y, 0);
                        var tile = tilemap.GetTile(pos);
                        if (tile == null) continue;

                        int ix = x;
                        int iy = y;
                        if (ix < 0 || iy < 0 || ix >= _gridSize.x || iy >= _gridSize.y)
                            continue;

                        bool isWater = tile != null && tilemap.name == "Water";
                        if (isWater)
                        {
                            _grids[GridLayer.Terrain][ix, iy] += 1; // 물은 통과 불가
                        }
                    }
                }
            }
        }

        protected override void OnTearDown()
        {
            _grids.Clear();
        }

        public void SetGridSize(Vector2Int gridSize)
        {
            _gridSize = gridSize;
        }

        public void SetPassable(GridLayer layer, Vector2Int position, Vector2Int size, bool isPassable)
        {
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    _grids[layer][position.x + x, position.y + y] += isPassable ? 1 : -1;
                }
            }
        }

        public bool IsPassable(GridLayer layer, Vector2Int position, Vector2Int size)
        {
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    try
                    {
                        if (_grids[layer][position.x + x, position.y + y] > 0)
                        {
                            return false;
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void PlaceTerrain(Vector2Int position, Vector2Int size)
            => SetPassable(GridLayer.Terrain, position, size, true);

        public void UnplaceTerrain(Vector2Int position, Vector2Int size)
            => SetPassable(GridLayer.Terrain, position, size, false);

        public void PlaceField(Vector2Int position, Vector2Int size)
            => SetPassable(GridLayer.Field, position, size, true);

        public void UnplaceField(Vector2Int position, Vector2Int size)
            => SetPassable(GridLayer.Field, position, size, false);
    }
}