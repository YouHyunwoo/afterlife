using Afterlife.Dev.World;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.Dev.Game
{
    public class Terrain : Moonstone.Ore.Local.Entity
    {
        [Header("Tilemap")]
        [SerializeField] private Tilemap _terrainObstacleTilemap;
        [SerializeField] private Tilemap _terrainLowerTilemap;
        [SerializeField] private Tilemap _terrainMiddleTilemap;
        [SerializeField] private Tilemap _terrainUpperTilemap;
        [SerializeField] private TileBase _waterRuleTile;
        [SerializeField] private TileBase _dirtRuleTile;
        [SerializeField] private TileBase _grassRuleTile;
        [SerializeField] private TileBase _snowRuleTile;

        [Header("Navigation")]
        [SerializeField] private NavMeshSurface _navMeshSurface;

        public void Render(Dev.World.TerrainLayer terrainLayer)
        {
            ClearTilemaps();
            SetUpTilemaps(terrainLayer);
        }

        private void ClearTilemaps()
        {
            _terrainObstacleTilemap.ClearAllTiles();
            _terrainLowerTilemap.ClearAllTiles();
            _terrainMiddleTilemap.ClearAllTiles();
            _terrainUpperTilemap.ClearAllTiles();
        }

        private bool SetUpTilemaps(Dev.World.TerrainLayer terrainLayer)
        {
            var worldMapSize = new Vector2Int(terrainLayer.Cells.GetLength(0), terrainLayer.Cells.GetLength(1));
            // if (!worldMap.GetLayerByType(WorldMapLayerType.Terrain, out Dev.World.TerrainLayer terrainLayer)) return false;

            for (int y = -1; y <= worldMapSize.y; y++)
            {
                for (int x = -1; x <= worldMapSize.x; x++)
                {
                    if (x < 0 || x >= worldMapSize.x || y < 0 || y >= worldMapSize.y)
                    {
                        _terrainLowerTilemap.SetTile(new Vector3Int(x, y), _waterRuleTile);
                        _terrainObstacleTilemap.SetTile(new Vector3Int(x, y), _waterRuleTile);
                        continue;
                    }

                    var position = new Vector3Int(x, y, 0);
                    var cell = terrainLayer.Cells[x, y];
                    switch (cell.Geography)
                    {
                        case GeographyType.Land:
                            SetLandTilesByBiome(cell, position);
                            _terrainLowerTilemap.SetTile(position, _dirtRuleTile);
                            break;
                        case GeographyType.Beach:
                            _terrainLowerTilemap.SetTile(position, _dirtRuleTile);
                            break;
                        case GeographyType.ShallowWater:
                        case GeographyType.DeepWater:
                            _terrainLowerTilemap.SetTile(position, _waterRuleTile);
                            _terrainObstacleTilemap.SetTile(position, _waterRuleTile);
                            break;
                    }
                }
            }

            return true;
        }

        private void SetLandTilesByBiome(TerrainCell cell, Vector3Int position)
        {
            switch (cell.Biome)
            {
                case BiomeType.IceSheet:
                    _terrainUpperTilemap.SetTile(position, _snowRuleTile);
                    break;
                case BiomeType.Tundra:
                case BiomeType.BorealForest:
                    _terrainMiddleTilemap.SetTile(position, _grassRuleTile);
                    _terrainUpperTilemap.SetTile(position, _snowRuleTile);
                    break;
                case BiomeType.Grassland:
                case BiomeType.Forest:
                case BiomeType.Savanna:
                case BiomeType.TropicalRainforest:
                    _terrainMiddleTilemap.SetTile(position, _grassRuleTile);
                    break;
            }
        }

        public void BuildNavMesh()
            => _navMeshSurface.BuildNavMesh();
    }
}