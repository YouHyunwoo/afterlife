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
        [SerializeField] private CameraSystem _cameraSystem;
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
        [SerializeField] private BuildingVisible _houseVisiblePrefab;
        [SerializeField] private ResourceVisible _treeVisiblePrefab;
        [SerializeField] private ResourceVisible _rockVisiblePrefab;
        [Space(10)]
        [SerializeField] private CitizenData _citizenData;
        [SerializeField] private EnemyData _goblinData;
        [SerializeField] private EnemyData _orcData;
        [SerializeField] private EnemyData _trollData;
        [SerializeField] private EnemyVisible _goblinVisiblePrefab;
        [SerializeField] private EnemyVisible _orcVisiblePrefab;
        [SerializeField] private EnemyVisible _trollVisiblePrefab;
        [SerializeField] private BuildingData _houseData;
        [SerializeField] private ResourceData _treeData;
        [SerializeField] private ResourceData _rockData;
        [Header("Initial Scatter")]
        [SerializeField] private int _initialTreeCount = 15;
        [SerializeField] private int _initialRockCount = 10;
        #endregion

        private WorldSystem _worldSystem;
        private WorldRepository _worldRepository;
        private WorldVisible _worldVisible;
        private ObjectRepository _objectRepository;
        private World.World _world;
        private readonly Dictionary<string, Citizen> _houseCitizenPairs = new();

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
                _cameraSystem,
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

            _buildMode.OnConfirmed += (position, buildParam, mode, sender) =>
            {
                if (buildParam.Prefab is BuildingVisible prefab)
                {
                    if (_objectSpawnSystem.TrySpawn(position, prefab, buildParam.Data, id => new Building(id), out var building, out var buildingVisible))
                    {
                        building.OnModeChanged += (mode, building, _) => buildingVisible.SetMode(mode.BuildingMode);
                        building.OnDied += (_, o, __) => _objectSpawnSystem.Despawn(o);
                        building.SetMode(BuildingMode.Preview);
                        _gameResultSystem.RegisterHouse(building);
                    }
                }
                _modeSystem.Select<SelectionMode>();
            };
            _buildMode.OnCanceled += (position, objectVisible, mode, sender) =>
            {
                _modeSystem.Select<SelectionMode>();
            };

            _worldSystem.OnWorldGeneratedAsync += _worldVisible.Render;
            _objectSpawnSystem.OnBuilt += (ov, shouldBuildNavMesh, system, sender) =>
            {
                if (shouldBuildNavMesh) _worldVisible.BuildNavMesh();

                if (ov is BuildingVisible buildingVisible)
                {
                    var building = buildingVisible.Object;
                    if (building.BuildingType == BuildingType.House)
                    {
                        var houseWorldPos = buildingVisible.transform.position;
                        building.OnBuilt += (b, _) => SpawnCitizenForHouse(b, houseWorldPos);
                        building.OnDied += (_, b, __) => DespawnCitizenForHouse((Building)b);
                    }
                }
            };
            _objectSpawnSystem.OnDemolished += async (ov, shouldBuildNavMesh, system, sender) =>
            {
                if (!shouldBuildNavMesh) return;
                await Awaitable.EndOfFrameAsync(); // Destroy() is deferred — wait for GameObject to be removed before baking
                _worldVisible.BuildNavMesh();
            };

            _gameResultSystem.OnGameClearEvent += () => Debug.Log("게임 클리어");
            _gameResultSystem.OnGameOverEvent += () => Debug.Log("게임 오버");
        }

        protected override async void PrepareObjects()
        {
            _world = await GenerateWorld();

            _cameraSystem.SetUp();
            _objectSpawnSystem.SetUp();

            // * 웨이브 스케줄 등록
            WaveScheduler.Schedule(_eventSystem, _container, new WaveSchedulerConfig
            {
                GoblinData = _goblinData, GoblinPrefab = _goblinVisiblePrefab,
                OrcData    = _orcData,    OrcPrefab    = _orcVisiblePrefab,
                TrollData  = _trollData,  TrollPrefab  = _trollVisiblePrefab,
            });

            // * 필드 오브젝트 생성 및 초기화
            var worldMap = _world.WorldMap;
            var passablePositions = worldMap.GetPassablePositions(Vector2Int.one);

            var housePassablePositions = worldMap.GetPassablePositions(_houseData.Size);
            var housePosition = SamplePassablePosition(housePassablePositions);
            if (_objectSpawnSystem.TrySpawn(housePosition, _houseVisiblePrefab, _houseData, id => new Building(id), out var house, out var houseVisible))
            {
                house.OnModeChanged += (m, _, _) => houseVisible.SetMode(m.BuildingMode);
                house.OnDied += (_, o, __) => _objectSpawnSystem.Despawn(o);
                house.FinishBuild();
                _gameResultSystem.RegisterHouse(house);
            }

            for (var i = 0; i < _initialTreeCount; i++)
            {
                if (passablePositions.Count == 0) break;
                var pos = SamplePassablePosition(passablePositions);
                if (_objectSpawnSystem.TrySpawn(pos, _treeVisiblePrefab, _treeData, id => new Resource(id), out var tree, out _))
                    tree.OnHarvested += (_, r, __) => _objectSpawnSystem.Despawn(r);
            }

            for (var i = 0; i < _initialRockCount; i++)
            {
                if (passablePositions.Count == 0) break;
                var pos = SamplePassablePosition(passablePositions);
                if (_objectSpawnSystem.TrySpawn(pos, _rockVisiblePrefab, _rockData, id => new Resource(id), out var rock, out _))
                    rock.OnHarvested += (_, r, __) => _objectSpawnSystem.Despawn(r);
            }

            var cameraPosition = houseVisible.transform.position;
            cameraPosition.z = Camera.main.transform.position.z;
            Camera.main.transform.position = cameraPosition;
        }

        private async Awaitable<World.World> GenerateWorld()
        {
            var world = await _worldSystem.GenerateWorldAsync(_generationParameter);
            if (world == null) return null;
            _worldRepository.Save(world);
            return world;
        }

        private void SpawnCitizenForHouse(Building house, Vector3 houseWorldPos)
        {
            var passablePositions = _world.WorldMap.GetPassablePositions(Vector2Int.one);
            if (passablePositions.Count == 0) return;

            var housePos = (Vector2)houseWorldPos;
            var citizenPosition = passablePositions.OrderBy(p => ((Vector2)p - housePos).sqrMagnitude).First();

            if (!_objectSpawnSystem.TrySpawn(citizenPosition, _citizenVisiblePrefab, _citizenData,
                id => new Citizen(id), out var citizen, out var citizenVisible)) return;

            citizen.Wander.IsPassable = _world.WorldMap.IsPassable;
            citizen.Wander.GetTownZonePositions = _world.WorldMap.GetTownZonePositions;
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

            _houseCitizenPairs[house.Id] = citizen;
            citizen.OnDied += (_, o, __) =>
            {
                _houseCitizenPairs.Remove(house.Id);
                _objectSpawnSystem.Despawn(o);
            };
        }

        private void DespawnCitizenForHouse(Building house)
        {
            if (_houseCitizenPairs.Remove(house.Id, out var citizen))
                _objectSpawnSystem.Despawn(citizen);
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
                _modeSystem.Select<BuildMode, BuildModeParam>(null, new BuildModeParam() { Prefab = _houseVisiblePrefab, Data = _houseData });
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Escape))
            {
                _modeSystem.Select<SelectionMode>();
            }
        }
    }
}
