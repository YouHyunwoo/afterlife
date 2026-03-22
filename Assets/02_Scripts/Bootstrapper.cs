using Afterlife.Dev.Agent;
using Afterlife.Dev.Field;
using Afterlife.Dev.Town;
using UnityEngine;

namespace Afterlife.Dev
{
    public class Bootstrapper : Moonstone.Ore.Bootstrapper
    {
        [SerializeField] private FieldNavigationSystem _fieldNavigationSystem;
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private ConstructionSystem _constructionSystem;
        [SerializeField] private MonsterSpawnSystem _monsterSpawnSystem;
        [SerializeField] private Citizen _citizen;
        [SerializeField] private HouseVisible _houseVisiblePrefab;
        [SerializeField] private BuildingData _houseData;
        [SerializeField] private ConstructionMode _constructionMode;

        protected override void CreateObjects()
        {
            var house = _constructionSystem.Build(new Vector2Int(2, 2), _houseVisiblePrefab, _houseData);
            _citizen.SetHouse(house.transform);
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
                _constructionSystem.Build(position, objectVisible, _houseData);
            };
            _constructionMode.OnCanceled += (position, objectVisible, mode, data) =>
            {
                Debug.Log("Construction canceled");
            };
        }

        protected override void PrepareObjects()
        {
            _fieldNavigationSystem.BuildNavMesh();
            // _monsterSpawnSystem.SpawnMonster(new Vector3(5, 5));
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