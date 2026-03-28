using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.Dev.World
{
    public class WorldSystem : Moonstone.Ore.Local.System
    {
        [Header("Tilemap")]
        [SerializeField] private Tilemap _terrainLowerTilemap;
        [SerializeField] private Tilemap _terrainMiddleTilemap;
        [SerializeField] private Tilemap _terrainUpperTilemap;
        [SerializeField] private Tilemap _terrainObstacleTilemap;
        [SerializeField] private TileBase _waterRuleTile;
        [SerializeField] private TileBase _dirtRuleTile;
        [SerializeField] private TileBase _grassRuleTile;
        [SerializeField] private TileBase _snowRuleTile;

        private WorldRepository _worldRepository;
        private World _world;

        public World World => _world;

        public bool GenerateWorld(WorldMapGenerationParameter generationParameter, out string worldId)
        {
            worldId = null;

            // * Model
            var generator = new WorldMapGenerator();
            if (!generator.Generate(generationParameter, out var worldMap)) return false;

            worldId = Moonstone.Ore.Model.NewId();
            var world = new World(worldId, worldMap);

            _world = world;
            _worldRepository.Save(world);

            // * View
            _terrainLowerTilemap.ClearAllTiles();
            _terrainMiddleTilemap.ClearAllTiles();
            _terrainUpperTilemap.ClearAllTiles();
            _terrainObstacleTilemap.ClearAllTiles();

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