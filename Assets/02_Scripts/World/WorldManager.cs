using Afterlife.Dev.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.Dev
{
    public class WorldManager : MonoBehaviour
    {
        [SerializeField] private WorldMapGenerationParameter _generationParameter;
        [SerializeField] private Tilemap _terrainTilemap;
        [SerializeField] private TileBase _dirtTile;
        [SerializeField] private TileBase _waterTile;
        [SerializeField] private bool _gizmosElevationEnabled = true;
        [SerializeField] private bool _gizmosTemperatureEnabled = false;
        [SerializeField] private bool _gizmosMoistureEnabled = false;
        [SerializeField] private Gradient _elevationGradient;
        [SerializeField] private Gradient _temperatureGradient;
        [SerializeField] private Gradient _moistureGradient;

        private World.Terrain terrain;

        private void Start()
        {
            var generator = new WorldMapGenerator();
            terrain = generator.Generate(_generationParameter);

            for (int y = 0; y < terrain.Size.y; y++)
            {
                for (int x = 0; x < terrain.Size.x; x++)
                {
                    var tile = terrain.Tiles[x, y];
                    switch (tile.Geography)
                    {
                        case GeographyType.Land:
                        case GeographyType.Beach:
                            _terrainTilemap.SetTile(new Vector3Int(x, y, 0), _dirtTile);
                            break;
                        case GeographyType.ShallowWater:
                        case GeographyType.DeepWater:
                            _terrainTilemap.SetTile(new Vector3Int(x, y, 0), _waterTile);
                            break;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (terrain == null) return;

            if (_gizmosElevationEnabled)
            {
                for (int y = 0; y < terrain.Size.y; y++)
                {
                    for (int x = 0; x < terrain.Size.x; x++)
                    {
                        var tile = terrain.Tiles[x, y];
                            Gizmos.color = _elevationGradient.Evaluate(tile.Elevation);
                            Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
            }

            if (_gizmosTemperatureEnabled)
            {
                for (int y = 0; y < terrain.Size.y; y++)
                {
                    for (int x = 0; x < terrain.Size.x; x++)
                    {
                        var tile = terrain.Tiles[x, y];
                            Gizmos.color = _temperatureGradient.Evaluate(tile.Temperature);
                            Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
            }

            if (_gizmosMoistureEnabled)
            {
                for (int y = 0; y < terrain.Size.y; y++)
                {
                    for (int x = 0; x < terrain.Size.x; x++)
                    {
                        var tile = terrain.Tiles[x, y];
                            Gizmos.color = _moistureGradient.Evaluate(tile.Moisture);
                            Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
            }
        }
    }
}