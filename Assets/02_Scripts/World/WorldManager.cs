using Afterlife.Dev.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.Dev
{
    public class WorldManager : MonoBehaviour
    {
        [Header("Tilemap")]
        [SerializeField] private Tilemap _terrainTilemap;
        [SerializeField] private TileBase _dirtRuleTile;
        [SerializeField] private TileBase _waterRuleTile;

        private World.Terrain _terrain;

        public World.Terrain Terrain => _terrain;

        public World.Terrain GenerateWorldMap(WorldMapGenerationParameter generationParameter)
        {
            var generator = new WorldMapGenerator();
            _terrain = generator.Generate(generationParameter);

            for (int y = -1; y <= _terrain.Size.y; y++)
            {
                for (int x = -1; x <= _terrain.Size.x; x++)
                {
                    if (x < 0 || x >= _terrain.Size.x || y < 0 || y >= _terrain.Size.y)
                    {
                        _terrainTilemap.SetTile(new Vector3Int(x, y, 0), _waterRuleTile);
                        continue;
                    }

                    var tile = _terrain.Tiles[x, y];
                    switch (tile.Geography)
                    {
                        case GeographyType.Land:
                        case GeographyType.Beach:
                            _terrainTilemap.SetTile(new Vector3Int(x, y, 0), _dirtRuleTile);
                            break;
                        case GeographyType.ShallowWater:
                        case GeographyType.DeepWater:
                            _terrainTilemap.SetTile(new Vector3Int(x, y, 0), _waterRuleTile);
                            break;
                    }
                }
            }

            return _terrain;
        }
    }
}