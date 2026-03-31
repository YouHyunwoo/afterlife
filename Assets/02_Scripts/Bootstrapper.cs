using Afterlife.Dev.Field;
using Afterlife.Dev.Mode;
using UnityEngine;
using Afterlife.Dev.Game;
using Afterlife.Dev.World;
using System.Collections.Generic;
using System.Linq;

namespace Afterlife.Dev
{
    public class Bootstrapper : Moonstone.Ore.Bootstrapper
    {
        [Header("Container")]
        [SerializeField] private Moonstone.Ore.Container _container;

        #region Models
        [Header("Models")]
        [SerializeField] private Player _player;
        #endregion

        #region Systems
        [Header("Systems")]
        [SerializeField] private ModeSystem _modeSystem;
        [SerializeField] private ObjectSpawnSystem _objectSpawnSystem;
        [SerializeField] private ObjectSystem _objectSystem;
        [SerializeField] private BuildGuideSystem _buildGuideSystem;
        [SerializeField] private TimeSystem _timeSystem;
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private GameResultSystem _gameResultSystem;
        [SerializeField] private WorldMapGenerationParameter _generationParameter;
        #endregion

        #region Modes
        [Header("Modes")]
        [SerializeField] private BuildMode _buildMode;
        [SerializeField] private SelectionMode _selectionMode;
        #endregion

        #region Objects
        [Header("Objects")]
        [SerializeField] private WorldVisible _worldVisiblePrefab;
        [SerializeField] private CitizenVisible _citizenVisiblePrefab;
        [SerializeField] private EnemyVisible _enemyVisiblePrefab;
        [SerializeField] private BuildingVisible _houseVisiblePrefab;
        [SerializeField] private ResourceVisible _treeVisiblePrefab;
        [Space(10)]
        [SerializeField] private CitizenData _citizenData;
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private BuildingData _houseData;
        [SerializeField] private ResourceData _treeData;
        #endregion

        private WorldSystem _worldSystem;
        private WorldRepository _worldRepository;
        private WorldVisible _worldVisible;
        private ObjectRepository _objectRepository;

        protected override void CreateObjects()
        {
            _worldSystem = new();
            _worldRepository = new();
            _worldVisible = Instantiate(_worldVisiblePrefab);
            _objectRepository = new();
        }

        protected override void InitializeObjects()
        {
            _modeSystem.Initialize();
            _modeSystem.Select<SelectionMode>();
        }

        protected override void BindObjects()
        {
            _container.Register(
                _container,
                _player,
                _modeSystem,
                _selectionMode,
                _buildMode,
                _worldSystem,
                _worldRepository,
                _objectSpawnSystem,
                _objectRepository,
                _buildGuideSystem,
                _timeSystem,
                _eventSystem,
                _gameResultSystem,
                _objectSystem
            );
            _container.AddInjectee(
            );
            _container.Bind();

            _buildMode.OnConfirmed += (position, objectVisiblePrefab, mode, sender) =>
            {
                _modeSystem.Select<SelectionMode>();
            };
            _buildMode.OnCanceled += (position, objectVisible, mode, sender) =>
            {
                _modeSystem.Select<SelectionMode>();
            };

            _worldSystem.OnWorldGeneratedAsync += _worldVisible.Render;
            _objectSpawnSystem.OnBuilt += (ov, shouldBuildNavMesh, system, sender) => { if (shouldBuildNavMesh) _worldVisible.BuildNavMesh(); };
            _objectSpawnSystem.OnDemolished += (ov, shouldBuildNavMesh, system, sender) => { if (shouldBuildNavMesh) _worldVisible.BuildNavMesh(); };

            _gameResultSystem.OnGameClearEvent += () => Debug.Log("게임 클리어");
            _gameResultSystem.OnGameOverEvent += () => Debug.Log("게임 오버");
        }

        protected override async void PrepareObjects()
        {
            var world = await GenerateWorld();

            _objectSpawnSystem.SetUp();

            // * 게임 이벤트 생성 및 등록
            {
                var enemySpawnEvent = new EnemySpawnEvent(5, _enemyVisiblePrefab);
                _container.Inject(enemySpawnEvent);
            }

            // * 필드 오브젝트 생성 및 초기화
            var worldMap = world.WorldMap;
            var passablePositions = worldMap.GetPassablePositions(Vector2Int.one);

            var housePassablePositions = worldMap.GetPassablePositions(_houseData.Size);
            var housePosition = SamplePassablePosition(housePassablePositions);
            if (_objectSpawnSystem.TrySpawn(housePosition, _houseVisiblePrefab, _houseData, id => new Building(id), out var house, out var houseVisible))
            {
                house.FinishBuild();
                _gameResultSystem.RegisterHouse(house);
            }

            var treePosition = SamplePassablePosition(passablePositions);
            if (_objectSpawnSystem.TrySpawn(treePosition, _treeVisiblePrefab, _treeData, id => new Resource(id), out var tree, out var treeVisible))
            {
                tree.OnHarvested += (harvestResultInfo, r, s) => _objectSpawnSystem.Despawn(r);
            }

            // * 시민 생성 및 초기화
            // TODO: 편의 메소드 필요
            var citizenPosition = SamplePassablePosition(passablePositions);
            if (_objectSpawnSystem.TrySpawn(citizenPosition, _citizenVisiblePrefab, _citizenData, id => new Citizen(id), out var citizen, out var citizenVisible))
            {
                citizen.Wander.IsPassable = world.WorldMap.IsPassable;
                citizen.Wander.GetTownZonePositions = world.WorldMap.GetTownZonePositions;
                citizen.BuildingLocator.GetBuildings = _objectRepository.FindAll().OfType<Building>;

                // 충돌 이벤트 → ObjectSystem 전달
                citizenVisible.OnInteractionCollided += (collider, field, cv, sender) =>
                {
                    var other = _objectSystem.GetModel(collider.gameObject);
                    if (other != null) _objectSystem.RegisterCollision(citizen, other);
                };

                citizen.OnHoldingsTaken += (woods, stones, c, s) => citizenVisible.TakeHoldings(woods, stones);
                citizen.OnHoldingsDropped += (c, s) => citizenVisible.DropHoldings();
                citizen.OnHoldingsReturned += (woods, stones, c, s) =>
                {
                    _player.Woods += woods;
                    _player.Stones += stones;
                    citizenVisible.DropHoldings();
                };
                citizen.OnDied += (a, o, s) => _objectSpawnSystem.Despawn(o);
            }

            // * 적 생성 및 초기화
            var enemyPosition = SamplePassablePosition(passablePositions);
            if (_objectSpawnSystem.TrySpawn(enemyPosition, _enemyVisiblePrefab, _enemyData, id => new Enemy(id), out var enemy, out var enemyVisible))
            {
                enemy.Wander.GetTownZonePositions += world.WorldMap.GetTownZonePositions;
                enemy.TargetScan.QueryObjects += _objectSystem.QueryObjects;
                enemy.OnDied += (a, o, s) =>
                {
                    _player.Aetheron += enemy.Aetheron;
                    _objectSpawnSystem.Despawn(o);
                };
            }
        }

        private async Awaitable<World.World> GenerateWorld()
        {
            var world = await _worldSystem.GenerateWorldAsync(_generationParameter);
            if (world == null) return null;
            _worldRepository.Save(world);

            var worldMapSize = world.WorldMap.Size;
            Camera.main.transform.position = new Vector3(worldMapSize.x / 2f, worldMapSize.y / 2f, -10f);
            Camera.main.orthographicSize = Mathf.Max(worldMapSize.x, worldMapSize.y) / 2f + 5f;

            return world;
        }

        private Vector2Int SamplePassablePosition(List<Vector2Int> passablePositions)
        {
            var index = Random.Range(0, passablePositions.Count);
            var passablePosition = passablePositions[index];
            passablePositions.RemoveAt(index);
            return passablePosition;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                _modeSystem.Select<BuildMode, BuildModeParam>(null, new BuildModeParam() { Prefab = _houseVisiblePrefab });
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Escape))
            {
                _modeSystem.Select<SelectionMode>();
            }
        }
    }
}
