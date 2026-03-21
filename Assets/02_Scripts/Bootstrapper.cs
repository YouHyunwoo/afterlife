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
        [SerializeField] private Citizen _citizen;
        [SerializeField] private House _housePrefab;

        protected override void CreateObjects()
        {
            var house = Instantiate(_housePrefab, new Vector3(0, 0) + new Vector3(0.5f, 0.5f), Quaternion.identity);
            house.Build(_townAreaSystem);
        }

        protected override void InitializeObjects()
        {
            _fieldNavigationSystem.Initialize();
            _townAreaSystem.Initialize();
        }

        protected override void BindObjects()
        {
        }

        protected override void PrepareObjects()
        {
            _fieldNavigationSystem.BuildNavMesh();
        }
    }
}