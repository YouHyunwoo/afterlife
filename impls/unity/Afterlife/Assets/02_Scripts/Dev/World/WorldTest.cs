using Afterlife.Dev.World;
using UnityEngine;

namespace Afterlife.Dev
{
    public class WorldTest : MonoBehaviour
    {
        private readonly Moonstone.Arc.DependencyInjection.ServiceResolver _resolver = new();
        private void OnDestroy() => _resolver.Dispose();

        [Header("World Map Generation")]
        [SerializeField] private WorldMapGenerationParameter _generationParameter;

        [Header("Debug")]
        [SerializeField] private bool _gizmosElevationEnabled = true;
        [SerializeField] private bool _gizmosTemperatureEnabled = false;
        [SerializeField] private bool _gizmosMoistureEnabled = false;
        [SerializeField] private Gradient _elevationGradient;
        [SerializeField] private Gradient _temperatureGradient;
        [SerializeField] private Gradient _moistureGradient;

        private WorldSystem _worldSystem;
        private WorldRepository _worldRepository;
        private string _worldId;

        private void Start()
        {
            _worldSystem = new();
            _worldRepository = new();

            _resolver.Register(_worldSystem);
            _resolver.Register(_worldRepository);
            _resolver.BindEachOther();

            GenerateWorldMap();
        }

        public void OnRefreshButtonClicked()
        {
            GenerateWorldMap();
        }

        private void GenerateWorldMap()
        {
            if (!_worldSystem.GenerateWorld(_generationParameter, out var world)) return;
            _worldRepository.Save(world);
            _worldId = world.Id;

            var worldMapSize = world.WorldMap.Size;
            Camera.main.transform.position = new Vector3(worldMapSize.x / 2f, worldMapSize.y / 2f, -10f);
            Camera.main.orthographicSize = Mathf.Max(worldMapSize.x, worldMapSize.y) / 2f + 5f;
        }

        private void OnDrawGizmos()
        {
            if (_worldId == null) return;

            if (!_worldRepository.TryFindById(_worldId, out var world)) return;
            if (!world.WorldMap.GetLayerByType(WorldMapLayerType.Terrain, out World.TerrainLayer terrainLayer)) return;

            var worldMapSize = world.WorldMap.Size;

            if (_gizmosMoistureEnabled)
            {
                for (int y = 0; y < worldMapSize.y; y++)
                {
                    for (int x = 0; x < worldMapSize.x; x++)
                    {
                        var cell = terrainLayer.Cells[x, y];
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
                        var cell = terrainLayer.Cells[x, y];
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
                        var cell = terrainLayer.Cells[x, y];
                        Gizmos.color = _elevationGradient.Evaluate(cell.Elevation);
                        Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one);
                    }
                }
                return;
            }
        }
    }
}