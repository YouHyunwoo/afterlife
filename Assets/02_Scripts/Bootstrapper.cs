using Afterlife.Dev.Field;
using Afterlife.Dev.Town;
using Afterlife.Dev.Mode;
using UnityEngine;

namespace Afterlife.Dev
{
    public class Bootstrapper : Moonstone.Ore.Bootstrapper
    {
        [SerializeField] private ModeSystem _modeSystem;
        [SerializeField] private RaycastSystem _raycastSystem;
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private NavigationSystem _navigationSystem;
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private ConstructionSystem _constructionSystem;
        [SerializeField] private MonsterSpawnSystem _monsterSpawnSystem;
        [SerializeField] private CitizenVisible _citizenVisiblePrefab;
        [SerializeField] private CitizenVisible _citizenVisible;
        [SerializeField] private HouseVisible _houseVisiblePrefab;
        [SerializeField] private BuildingData _houseData;
        [SerializeField] private ResourceVisible _treeVisiblePrefab;
        [SerializeField] private ResourceData _treeData;

        [Header("Modes")]
        [SerializeField] private ConstructionMode _constructionMode;
        [SerializeField] private SelectionMode _selectionMode;

        protected override void CreateObjects()
        {
            // _citizenVisible = Instantiate(_citizenVisiblePrefab);
            // _citizenVisible.SetTownAreaSystem(_townAreaSystem);
            // _citizenVisible.SetGridSytem(_gridSystem);
        }

        protected override void InitializeObjects()
        {
            _modeSystem.Initialize();
            _modeSystem.Select<SelectionMode>();
        }

        protected override void BindObjects()
        {
            _constructionMode.OnConfirmed += (position, objectVisible, mode, sender) =>
            {
                var result = _constructionSystem.TryBuild(position, objectVisible, _houseData, out _);
                _navigationSystem.BuildNavMesh();
                _modeSystem.Select<SelectionMode>();
            };
            _constructionMode.OnCanceled += (position, objectVisible, mode, sender) =>
            {
                Debug.Log("Construction canceled");
                _modeSystem.Select<SelectionMode>();
            };

            _selectionMode.OnSelected += (position, mode, sender) =>
            {
                _citizenVisible.DoCommand(CommandType.Move, new object[] { position });
            };
        }

        protected override void PrepareObjects()
        {
            _gridSystem.SetGridSize(new Vector2Int(20, 17));
            _gridSystem.SetUp();

            var house = _constructionSystem.Build(new Vector2Int(2, 2), _houseVisiblePrefab, _houseData);
            _constructionSystem.Build(new Vector2Int(2, 5), _treeVisiblePrefab, _treeData);

            // _monsterSpawnSystem.SpawnMonster(new Vector3(5, 5));

            _navigationSystem.BuildNavMesh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                _modeSystem.Select<ConstructionMode, ConstructionModeParam>(null, new ConstructionModeParam() { ObjectVisiblePrefab = _houseVisiblePrefab });
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Escape))
            {
                _modeSystem.Select<SelectionMode>();
            }
        }
    }
}