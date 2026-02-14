using Afterlife.Dev.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.Dev
{
    public class WorldManager : MonoBehaviour
    {
        [Header("Tilemap")]
        [SerializeField] private Tilemap _terrainLowerTilemap;
        [SerializeField] private Tilemap _terrainMiddleTilemap;
        [SerializeField] private Tilemap _terrainUpperTilemap;
        [SerializeField] private TileBase _waterRuleTile;
        [SerializeField] private TileBase _dirtRuleTile;
        [SerializeField] private TileBase _grassRuleTile;
        [SerializeField] private TileBase _snowRuleTile;

        private World.Terrain _terrain;

        public World.Terrain Terrain => _terrain;

        public World.Terrain GenerateWorldMap(WorldMapGenerationParameter generationParameter)
        {
            _terrainLowerTilemap.ClearAllTiles();
            _terrainMiddleTilemap.ClearAllTiles();
            _terrainUpperTilemap.ClearAllTiles();

            var generator = new WorldMapGenerator();
            _terrain = generator.Generate(generationParameter);

            for (int y = -1; y <= _terrain.Size.y; y++)
            {
                for (int x = -1; x <= _terrain.Size.x; x++)
                {
                    if (x < 0 || x >= _terrain.Size.x || y < 0 || y >= _terrain.Size.y)
                    {
                        _terrainLowerTilemap.SetTile(new Vector3Int(x, y, 0), _waterRuleTile);
                        continue;
                    }

                    var position = new Vector3Int(x, y, 0);
                    var tile = _terrain.Tiles[x, y];
                    switch (tile.Geography)
                    {
                        case GeographyType.Land:
                            TileLandByBiome(tile, position);
                            _terrainLowerTilemap.SetTile(position, _dirtRuleTile);
                            break;
                        case GeographyType.Beach:
                            _terrainLowerTilemap.SetTile(position, _dirtRuleTile);
                            break;
                        case GeographyType.ShallowWater:
                        case GeographyType.DeepWater:
                            _terrainLowerTilemap.SetTile(position, _waterRuleTile);
                            break;
                    }
                }
            }

            return _terrain;
        }

        private void TileLandByBiome(World.Tile tile, Vector3Int position)
        {
            switch (tile.Biome)
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