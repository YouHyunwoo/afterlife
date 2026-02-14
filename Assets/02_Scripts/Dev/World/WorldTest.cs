using UnityEngine;

namespace Afterlife.Dev
{
    public class WorldTest : MonoBehaviour
    {
        [Header("World Map Generation")]
        [SerializeField] private WorldManager _worldManager;
        [SerializeField] private WorldMapGenerationParameter _generationParameter;

        [Header("Debug")]
        [SerializeField] private bool _gizmosElevationEnabled = true;
        [SerializeField] private bool _gizmosTemperatureEnabled = false;
        [SerializeField] private bool _gizmosMoistureEnabled = false;
        [SerializeField] private Gradient _elevationGradient;
        [SerializeField] private Gradient _temperatureGradient;
        [SerializeField] private Gradient _moistureGradient;

        private World.Terrain _terrain;

        private void Start()
        {
            _terrain = _worldManager.GenerateWorldMap(_generationParameter);
        }

        private void OnDrawGizmos()
        {
            if (_terrain == null) return;

            if (_gizmosElevationEnabled)
            {
                for (int y = 0; y < _terrain.Size.y; y++)
                {
                    for (int x = 0; x < _terrain.Size.x; x++)
                    {
                        var tile = _terrain.Tiles[x, y];
                            Gizmos.color = _elevationGradient.Evaluate(tile.Elevation);
                            Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
            }

            if (_gizmosTemperatureEnabled)
            {
                for (int y = 0; y < _terrain.Size.y; y++)
                {
                    for (int x = 0; x < _terrain.Size.x; x++)
                    {
                        var tile = _terrain.Tiles[x, y];
                            Gizmos.color = _temperatureGradient.Evaluate(tile.Temperature);
                            Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
            }

            if (_gizmosMoistureEnabled)
            {
                for (int y = 0; y < _terrain.Size.y; y++)
                {
                    for (int x = 0; x < _terrain.Size.x; x++)
                    {
                        var tile = _terrain.Tiles[x, y];
                            Gizmos.color = _moistureGradient.Evaluate(tile.Moisture);
                            Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
            }
        }
    }
}