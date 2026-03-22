using Afterlife.Dev.Agent;
using Afterlife.Dev.Field;
using Afterlife.Dev.Town;
using UnityEngine;

namespace Afterlife.Dev
{
    public class Bootstrapper : Moonstone.Ore.Bootstrapper
    {
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private FieldNavigationSystem _fieldNavigationSystem;
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private ConstructionSystem _constructionSystem;
        [SerializeField] private MonsterSpawnSystem _monsterSpawnSystem;
        [SerializeField] private Citizen _citizen;
        [SerializeField] private HouseVisible _houseVisiblePrefab;
        [SerializeField] private BuildingData _houseData;
        [SerializeField] private ResourceVisible _treeVisiblePrefab;
        [SerializeField] private ResourceData _treeData;

        [Header("Modes")]
        [SerializeField] private ConstructionMode _constructionMode;

        protected override void CreateObjects()
        {
        }

        protected override void InitializeObjects()
        {
            _fieldNavigationSystem.Initialize();
            _townAreaSystem.Initialize();
            _monsterSpawnSystem.Initialize();
            _constructionMode.Initialize();
            _constructionMode.Exit();
        }

        protected override void BindObjects()
        {
            _constructionMode.OnConfirmed += (position, objectVisible, mode, data) =>
            {
                var result = _constructionSystem.TryBuild(position, objectVisible, _houseData, out _);
                _fieldNavigationSystem.BuildNavMesh();
            };
            _constructionMode.OnCanceled += (position, objectVisible, mode, data) =>
            {
                Debug.Log("Construction canceled");
            };
        }

        protected override void PrepareObjects()
        {
            _gridSystem.SetGridSize(new Vector2Int(20, 17));
            _gridSystem.SetUp();

            var house = _constructionSystem.Build(new Vector2Int(2, 2), _houseVisiblePrefab, _houseData);
            _constructionSystem.Build(new Vector2Int(2, 5), _treeVisiblePrefab, _treeData);
            _citizen.SetHouse(house.transform);

            // _monsterSpawnSystem.SpawnMonster(new Vector3(5, 5));

            _fieldNavigationSystem.BuildNavMesh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                _constructionMode.Enter(_houseVisiblePrefab);
            }
        }
    }
}