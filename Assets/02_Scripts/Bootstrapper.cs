using Afterlife.Dev.Field;
using Afterlife.Dev.Mode;
using UnityEngine;
using Afterlife.Dev.Game;

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
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private NavigationSystem _navigationSystem;
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private BuildSystem _buildSystem;
        [SerializeField] private TimeSystem _timeSystem;
        [SerializeField] private EventSystem _eventSystem;
        #endregion

        #region Modes
        [Header("Modes")]
        [SerializeField] private BuildMode _buildMode;
        [SerializeField] private SelectionMode _selectionMode;
        #endregion

        #region Objects
        [Header("Objects")]
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

        protected override void CreateObjects()
        {
            // 오브젝트 생성 흐름:
            // 오브젝트 모델 생성 및 저장 -> 오브젝트 Visible 생성 -> 바인딩
            // 오브젝트 == 건물: 필드 그리드 적용, 네비게이션 빌드
            // _citizenVisible = Instantiate(_citizenVisiblePrefab);
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
                _raycastSystem,
                _gridSystem,
                _navigationSystem,
                _townAreaSystem,
                _buildSystem,
                _timeSystem,
                _eventSystem
            );
            _container.AddInjectee(
                _citizenVisible,
                _enemyVisible
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
        }

        protected override void PrepareObjects()
        {
            _gridSystem.SetGridSize(new Vector2Int(20, 17));
            _gridSystem.SetUp();

            // * 게임 이벤트 생성 및 등록
            {
                var enemySpawnEvent = new EnemySpawnEvent(5, _enemyVisiblePrefab);
                _container.Inject(enemySpawnEvent);
                _eventSystem.Register(enemySpawnEvent);
            }

            if (_buildSystem.TryBuild(new Vector2Int(2, 2), _houseVisiblePrefab, _houseData, out var houseVisible))
            {
                houseVisible.FinishBuild();
            }

            if (_buildSystem.TryBuild(new Vector2Int(2, 9), _treeVisiblePrefab, _treeData, out var tree))
            {
                tree.OnHarvested += (resourcePrefab, resourceVisible, sender) => _buildSystem.Demolish(resourceVisible);
            }

            _navigationSystem.BuildNavMesh();


            // * 적 생성 및 바인딩, 초기화
            _enemyVisible = Instantiate(_enemyVisiblePrefab);
            _container.Inject(_enemyVisible);
            for (var i = 0; i < 100; i++)
            {
                var enemyPosition = new Vector2(Random.Range(0, 20), Random.Range(0, 20));
                if (_townAreaSystem.IsPositionInAnyInfluence(enemyPosition))
                    continue;
                _enemyVisible.NavMeshAgent.Warp(enemyPosition);
                break;
            }
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