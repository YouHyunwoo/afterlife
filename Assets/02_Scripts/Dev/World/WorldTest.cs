using Afterlife.Dev.World;
using UnityEngine;

namespace Afterlife.Dev
{
    public class WorldTest : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private Moonstone.Ore.Container _container;

        [Header("World Map Generation")]
        [SerializeField] private WorldMapGenerationParameter _generationParameter;

        [Header("Debug")]
        [SerializeField] private bool _gizmosElevationEnabled = true;
        [SerializeField] private bool _gizmosTemperatureEnabled = false;
        [SerializeField] private bool _gizmosMoistureEnabled = false;
        [SerializeField] private Gradient _elevationGradient;
        [SerializeField] private Gradient _temperatureGradient;
        [SerializeField] private Gradient _moistureGradient;

        // private WorldSystem _worldSystem;
        private Game.World _world;
        // private string _worldId;

        private void Start()
        {
            _container.Register(
                _world
            );
            _container.AddInjectee();
            _container.Bind();

            GenerateWorldMap();
        }

        public void OnRefreshButtonClicked()
        {
            GenerateWorldMap();
        }

        private void GenerateWorldMap()
        {
            var generationResult = _world.Generate(_generationParameter);
            if (generationResult == null) return;

            var worldMapSize = _world.Size;
            Camera.main.transform.position = new Vector3(worldMapSize.x / 2f, worldMapSize.y / 2f, -10f);
            Camera.main.orthographicSize = Mathf.Max(worldMapSize.x, worldMapSize.y) / 2f + 5f;
        }

        private void OnDrawGizmos()
        {
            if (_world == null) return;

            var worldMapSize = _world.Size;

            if (_gizmosMoistureEnabled)
            {
                for (int y = 0; y < worldMapSize.y; y++)
                {
                    for (int x = 0; x < worldMapSize.x; x++)
                    {
                        var cell = _world.TerrainLayer.Cells[x, y];
                        Gizmos.color = _moistureGradient.Evaluate(cell.Moisture);
                        Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
                return;
            }

            if (_gizmosTemperatureEnabled)
            {
                for (int y = 0; y < worldMapSize.y; y++)
                {
                    for (int x = 0; x < worldMapSize.x; x++)
                    {
                        var cell = _world.TerrainLayer.Cells[x, y];
                        Gizmos.color = _temperatureGradient.Evaluate(cell.Temperature);
                        Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
                return;
            }

            if (_gizmosElevationEnabled)
            {
                for (int y = 0; y < worldMapSize.y; y++)
                {
                    for (int x = 0; x < worldMapSize.x; x++)
                    {
                        var cell = _world.TerrainLayer.Cells[x, y];
                        Gizmos.color = _elevationGradient.Evaluate(cell.Elevation);
                        Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
                return;
            }
        }
    }
}