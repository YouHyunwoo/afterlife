using Afterlife.Dev.Field;
using Afterlife.Dev.Mode;
using UnityEngine;
using Afterlife.Dev.Game;
using Afterlife.Dev.World;
using System.Collections.Generic;

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
        [SerializeField] private RaycastSystem _raycastSystem;
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private BuildSystem _buildSystem;
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
        [SerializeField] private HouseVisible _houseVisiblePrefab;
        [SerializeField] private ResourceVisible _treeVisiblePrefab;
        [Space(10)]
        [SerializeField] private CitizenVisible _citizenVisible;
        // [SerializeField] private CitizenData _citizenData;
        [SerializeField] private EnemyVisible _enemyVisible;
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private BuildingData _houseData;
        [SerializeField] private ResourceData _treeData;
        #endregion

        private WorldSystem _worldSystem;
        private WorldRepository _worldRepository;
        private WorldVisible _worldVisible;

        protected override void CreateObjects()
        {
            _worldSystem = new();
            _worldRepository = new();
            _worldVisible = Instantiate(_worldVisiblePrefab);
            // 오브젝트 생성 흐름:
            // 오브젝트 모델 생성 및 저장 -> 오브젝트 Visible 생성 -> 바인딩
            // 오브젝트 == 건물: 필드 그리드 적용, 네비게이션 빌드
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
                _raycastSystem,
                _worldSystem,
                _worldRepository,
                _townAreaSystem,
                _buildSystem,
                _buildGuideSystem,
                _timeSystem,
                _eventSystem,
                _gameResultSystem
            );
            _container.AddInjectee(
            );
            _container.Bind();

            _buildMode.OnConfirmed += (position, objectVisiblePrefab, mode, sender) =>
            {
                _buildSystem.TryBuild(position, objectVisiblePrefab, _houseData, out var objectVisible);
                _modeSystem.Select<SelectionMode>();
            };
            _buildMode.OnCanceled += (position, objectVisible, mode, sender) =>
            {
                _modeSystem.Select<SelectionMode>();
            };

            _worldSystem.OnWorldGeneratedAsync += _worldVisible.Render;
            _buildSystem.OnBuilt += (objectVisible, buildSystem, sender) => { if (objectVisible.Size != Vector2Int.zero) _worldVisible.BuildNavMesh(); };
            _buildSystem.OnDemolished += (objectVisible, buildSystem, sender) => { if (objectVisible.Size != Vector2Int.zero) _worldVisible.BuildNavMesh(); };

            _gameResultSystem.OnGameClearEvent += () => Debug.Log("게임 클리어");
            _gameResultSystem.OnGameOverEvent += () => Debug.Log("게임 오버");
        }

        protected override async void PrepareObjects()
        {
            var world = await GenerateWorld();

            _buildSystem.SetUp();

            // * 게임 이벤트 생성 및 등록
            {
                var enemySpawnEvent = new EnemySpawnEvent(5, _enemyVisiblePrefab);
                _container.Inject(enemySpawnEvent);
                // _eventSystem.Register(enemySpawnEvent);
            }

            // * 필드 오브젝트 생성 및 초기화
            var worldMap = world.WorldMap;
            var worldMapSize = worldMap.Size;
            var passablePositions = worldMap.GetPassablePositions(Vector2Int.one);

            var housePassablePositions = worldMap.GetPassablePositions(_houseData.Size);
            var housePosition = SamplePassablePosition(housePassablePositions);
            if (_buildSystem.TryBuild(housePosition, _houseVisiblePrefab, _houseData, out var houseVisible))
            {
                houseVisible.FinishBuild();
                _gameResultSystem.RegisterHouse(houseVisible);
            }

            var treePosition = SamplePassablePosition(passablePositions);
            if (_buildSystem.TryBuild(treePosition, _treeVisiblePrefab, _treeData, out var tree))
            {
                tree.OnHarvested += (resourcePrefab, resourceVisible, sender) => _buildSystem.Demolish(resourceVisible);
            }

            // * 시민 생성 및 초기화
            var citizenPosition = SamplePassablePosition(passablePositions);
            _citizenVisible = Instantiate(_citizenVisiblePrefab, (Vector2)citizenPosition, Quaternion.identity);
            _container.Inject(_citizenVisible);
            _citizenVisible.IsPassable += world.WorldMap.IsPassable;
            _citizenVisible.NavMeshAgent.Warp((Vector2)citizenPosition);

            // * 적 생성 및 초기화
            var enemyPosition = SamplePassablePosition(passablePositions);
            _enemyVisible = Instantiate(_enemyVisiblePrefab, (Vector2)enemyPosition, Quaternion.identity);
            _container.Inject(_enemyVisible);
            _enemyVisible.OnDied += (attacker, ov, sender) =>
            {
                if (ov is EnemyVisible enemyVisible)
                    _player.Aetheron += enemyVisible.Aetheron;
            };
            _gameResultSystem.RegisterBoss(_enemyVisible);
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
                _modeSystem.Select<BuildMode, BuildModeParam>(null, new BuildModeParam() { ObjectVisiblePrefab = _houseVisiblePrefab });
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Escape))
            {
                _modeSystem.Select<SelectionMode>();
            }
        }
    }
}