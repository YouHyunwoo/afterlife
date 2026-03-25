using Afterlife.Dev.Field;
using Afterlife.Dev.Mode;
using UnityEngine;
using Afterlife.Dev.Game;

namespace Afterlife.Dev
{
    public class Bootstrapper : Moonstone.Ore.Bootstrapper
    {
        [Header("Instances")]
        [SerializeField] private Player _playerInstance;

        [Header("Systems")]
        [SerializeField] private ModeSystem _modeSystem;
        [SerializeField] private RaycastSystem _raycastSystem;
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private NavigationSystem _navigationSystem;
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private BuildSystem _buildSystem;
        [SerializeField] private MonsterSpawnSystem _monsterSpawnSystem;

        [Header("Objects")]
        [SerializeField] private CitizenVisible _citizenVisiblePrefab;
        [SerializeField] private CitizenVisible _citizenVisible;
        [SerializeField] private HouseVisible _houseVisiblePrefab;
        [SerializeField] private BuildingData _houseData;
        [SerializeField] private ResourceVisible _treeVisiblePrefab;
        [SerializeField] private ResourceData _treeData;

        [Header("Modes")]
        [SerializeField] private BuildMode _buildMode;
        [SerializeField] private SelectionMode _selectionMode;

        protected override void CreateObjects()
        {
            // _citizenVisible = Instantiate(_citizenVisiblePrefab);
            // _citizenVisible.SetTownAreaSystem(_townAreaSystem);
            // _citizenVisible.SetGridSytem(_gridSystem);
            // _citizenVisible.SetBuildSystem(_buildSystem);
        }

        protected override void InitializeObjects()
        {
            _modeSystem.Initialize();
            _modeSystem.Select<SelectionMode>();
        }

        protected override void BindObjects()
        {
            Globals.Player = _playerInstance;

            _buildMode.OnConfirmed += (position, objectVisiblePrefab, mode, sender) =>
            {
                _buildSystem.TryBuild(position, objectVisiblePrefab, _houseData, out var objectVisible);
                if (objectVisible is BuildingVisible buildingVisible)
                {
                    buildingVisible.OnCommanded += _selectionMode.HandleBuildingCommanded;
                }
                _modeSystem.Select<SelectionMode>();
            };
            _buildMode.OnCanceled += (position, objectVisible, mode, sender) =>
            {
                _modeSystem.Select<SelectionMode>();
            };

            _selectionMode.OnSelected += (position, mode, sender) =>
            {
                _citizenVisible.DoCommand(CommandType.Move, new object[] { position });
            };
        }

        protected override void PrepareObjects()
        {
            _playerInstance.Initialize();

            _gridSystem.SetGridSize(new Vector2Int(20, 17));
            _gridSystem.SetUp();

            if (_buildSystem.TryBuild(new Vector2Int(2, 2), _houseVisiblePrefab, _houseData, out var houseVisible))
            {
                houseVisible.FinishBuild();
            }

            if (_buildSystem.TryBuild(new Vector2Int(2, 9), _treeVisiblePrefab, _treeData, out var tree))
            {
                tree.OnCommanded += _selectionMode.HandleResourceCommanded;
                tree.OnHarvested += (resourcePrefab, resourceVisible, sender) => _buildSystem.Demolish(resourceVisible);
            }

            // _monsterSpawnSystem.SpawnMonster(new Vector3(5, 5));

            _navigationSystem.BuildNavMesh();

            _selectionMode.AddSelectedObjects(new ObjectVisible[] { _citizenVisible });
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