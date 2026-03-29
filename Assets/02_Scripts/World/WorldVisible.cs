using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.Dev.World
{
    public class WorldVisible : Moonstone.Ore.Local.Visible
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

        #region Caching
        private World _world;
        private IWorldMapLayer _terrainLayer;
        private IWorldMapLayer _fieldLayer;
        private TownLayer _townLayer;
        #endregion

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

            if (_world.WorldMap.TownAreaMap.Count == 0) return;

            var offset = new Vector3(0.5f, 0.5f);
            Gizmos.color = new Color(1, 0, 1, 0.3f);
            foreach (var cell in _world.WorldMap.TownAreaMap.Keys)
            {
                var position = (Vector3)(Vector2)cell + offset;
                Gizmos.DrawCube(position, Vector3Int.one);
            }
        }

        public async Awaitable Render(World world)
        {
            _world = world;
            _world.WorldMap.GetLayerByType(WorldMapLayerType.Terrain, out _terrainLayer);
            _world.WorldMap.GetLayerByType(WorldMapLayerType.Field, out _fieldLayer);

            ClearTilemaps();
            SetUpTilemaps(world.WorldMap);

            await Awaitable.EndOfFrameAsync();

            await _navMeshSurface.BuildNavMeshAsync();
        }

        public void BuildNavMesh()
            => _navMeshSurface.BuildNavMesh();

        private void ClearTilemaps()
        {
            _terrainObstacleTilemap.ClearAllTiles();
            _terrainLowerTilemap.ClearAllTiles();
            _terrainMiddleTilemap.ClearAllTiles();
            _terrainUpperTilemap.ClearAllTiles();
        }

        private bool SetUpTilemaps(WorldMap worldMap)
        {
            var worldMapSize = worldMap.Size;
            if (!worldMap.GetLayerByType(WorldMapLayerType.Terrain, out TerrainLayer terrainLayer)) return false;

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
    }
}