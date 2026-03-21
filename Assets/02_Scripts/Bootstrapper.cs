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
        [SerializeField] private MonsterSpawnSystem _monsterSpawnSystem;
        [SerializeField] private Citizen _citizen;
        [SerializeField] private House _housePrefab;

        protected override void CreateObjects()
        {
            var house = Instantiate(_housePrefab, new Vector3(2, 2), Quaternion.identity);
            house.Build(_townAreaSystem);

            _citizen.SetHouse(house.transform);
        }

        protected override void InitializeObjects()
        {
            _fieldNavigationSystem.Initialize();
            _townAreaSystem.Initialize();
            _monsterSpawnSystem.Initialize();

        }

        protected override void BindObjects()
        {
        }

        protected override void PrepareObjects()
        {
            _fieldNavigationSystem.BuildNavMesh();
            _monsterSpawnSystem.SpawnMonster(new Vector3(5, 5));
        }
    }
}