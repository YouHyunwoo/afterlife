using Afterlife.Dev.Mode;
using UnityEngine;
using Afterlife.Dev.Game;
using System.Collections.Generic;
using System.Linq;
using ECS.Dev;
using BuildingData = Afterlife.Dev.Game.BuildingData;
using ResourceData = Afterlife.Dev.Game.ResourceData;
using UnityEngine.AI;
using System.Threading.Tasks;

namespace Afterlife.Dev
{
    public class Bootstrapper : Moonstone.Ore.Bootstrapper
    {
        [Header("Container")]
        [SerializeField] private Moonstone.Ore.Container _container;

        #region Systems
        [Header("Systems")]
        [SerializeField] private ModeSystem _modeSystem;
        [SerializeField] private SpriteRenderer _buildGuidePrefab;
        [SerializeField] private WorldMapGenerationParameter _generationParameter;
        #endregion

        #region Modes
        [Header("Modes")]
        [SerializeField] private BuildMode _buildMode;
        [SerializeField] private SelectMode _selectMode;
        #endregion

        #region Objects
        [Header("Objects")]
        [SerializeField] private Game.World _worldPrefab;
        [SerializeField] private BuildingData _houseData;
        [SerializeField] private ResourceData _treeData;
        [SerializeField] private ResourceData _rockData;
        [SerializeField] private CitizenData _citizenData;
        [SerializeField] private EnemyData _enemyData;

        [Header("Initial Scatter")]
        [SerializeField] private int _initialTreeCount = 15;
        [SerializeField] private int _initialRockCount = 10;
        #endregion

        private Game.World _world;
        private bool isReady;

        #region ECS
        private ComponentManager componentManager;
        private EntityManager entityManager;
        private EntityFactory entityFactory;
        private Application<ComponentManager> application;
        private List<int> spawnedBuildingIds = new();
        private BuildGuideSystem buildGuideSystem;
        #endregion

        protected override void CreateObjects()
        {
            _world = Instantiate(_worldPrefab);
            componentManager = new();
            entityManager = new();
            entityFactory = new();
            buildGuideSystem = new BuildGuideSystem(_buildGuidePrefab);
        }

        protected override void InitializeObjects()
        {
            componentManager.Game.World = _world;
            componentManager.Game.Time = 0f;
            componentManager.Game.TimeScale = 1f;

            _modeSystem.Initialize();
            _modeSystem.Select<SelectMode>();
            _buildMode.SetBuildGuideSystem(buildGuideSystem);
        }

        protected override void BindObjects()
        {
            _container.Register(
                _container,
                _modeSystem,
                _selectMode,
                _buildMode,
                _world,
                componentManager,
                entityManager,
                entityFactory
            );
            _container.AddInjectee(
            );
            _container.Bind();

            _buildMode.OnConfirmed += (position, buildParam, mode, sender) =>
            {
                if (_world.TrySpawn(position, buildParam.Data, out var entity))
                {
                    if (componentManager.HasBuildingModeSetter[entity.Id] &&
                        componentManager.HasBuildingTag[entity.Id])
                    {
                        var buildingModeSetter = componentManager.BuildingModeSetters[entity.Id];
                        buildingModeSetter.SetMode(BuildingMode.Preview);
                        var building = componentManager.Buildings[entity.Id];
                        building.Mode = BuildingMode.Preview;
                    }
                }
                _modeSystem.Select<SelectMode>();
            };
            _buildMode.OnCanceled += (position, buildParam, mode, sender) =>
            {
                _modeSystem.Select<SelectMode>();
            };

            entityFactory.OnSpawned += async (info, _) =>
            {
                if (componentManager.HasBuildingTag[info.EntityId])
                    spawnedBuildingIds.Add(info.EntityId);
                if (isReady && info.HasVolume)
                    await RebuildNavMesh();
            };
            entityFactory.OnDespawned += async (info, _) =>
            {
                if (componentManager.HasBuildingTag[info.EntityId])
                    spawnedBuildingIds.Remove(info.EntityId);
                if (isReady && info.HasVolume)
                    await RebuildNavMesh();
            };

            entityManager.OnEntityDestroyed += componentManager.HandleEntityDestroyed;
        }

        private async Task RebuildNavMesh()
        {
            await Awaitable.EndOfFrameAsync();
            _world.BuildNavMesh();

            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!componentManager.HasEntity[i]) continue;
                if (!componentManager.HasNavMeshAgent[i]) continue;
                var agent = componentManager.NavMeshAgents[i];
                if (agent.isActiveAndEnabled)
                    agent.Warp(componentManager.Entities[i].transform.position);
            }
        }

        protected override async void PrepareObjects()
        {
            var gameResultSystem = new GameResultSystem();
            gameResultSystem.OnGameClear += () => Debug.Log("ECS 게임 클리어");
            gameResultSystem.OnGameOver  += () => Debug.Log("ECS 게임 오버");

            var buildingSystem = new BuildingSystem();
            buildingSystem.OnBuilt += (entityId) =>
            {
                if (componentManager.HasHouseTag[entityId] &&
                    componentManager.HasEntity[entityId])
                {
                    SpawnCitizenForHouse(componentManager.Entities[entityId].transform.position);
                }
            };

            application = new (
                new ISystem<ComponentManager>[]
                {
                    new CameraSystem(Camera.main),
                    new TimeSystem(),
                    new EventSystem(),
                    new HealthSystem(),
                    new MoveSystem(),
                    buildingSystem,
                    new ArrivalSystem(),
                    new WanderSystem
                    {
                        GetTownZonePositions = _world.GetTownZonePositions,
                        IsPassable = _world.IsPassable,
                    },
                    new SelectSystem(),
                    new DetectionSystem(),
                    new ChaseSystem(),
                    new FightSystem(),
                    new HarvestSystem(),
                    new ReturnSystem()
                    {
                        GetBuildings = () => spawnedBuildingIds,
                    },
                    new BuildSystem(),
                    new EnemyMoveSystem(),
                    new EnemyFightSystem(),
                    gameResultSystem,
                    new FogSystem()
                    {
                        GetFog = (pos) => _world.FogLayer.GetFog(pos),
                        SetFog = (pos, fog) => _world.FogLayer.SetFog(pos, fog),
                        SetRendererAlpha = (pos, color) => _world.Fog.SetRendererAlpha(pos, color),
                    },
                    new FogAlphaSystem()
                    {
                        GetFogCells = () => _world.FogLayer.Cells,
                    },
                },
                componentManager
            );

            var generationResult = _world.Generate(_generationParameter);
            if (generationResult == null) return;

            // * 웨이브 스케줄 등록
            // WaveScheduler.Schedule(componentManager, _world, new WaveSchedulerConfig
            // {
            //     GoblinData = _goblinData,
            //     OrcData    = _orcData,
            //     TrollData  = _trollData,
            // });

            // * 필드 오브젝트 생성 및 초기화
            var housePassablePositions = _world.GetPassablePositions(_houseData.Volume2DSize);
            var housePosition = SamplePassablePosition(housePassablePositions);
            if (_world.TrySpawn(housePosition, _houseData, out var houseEntity))
            {
                componentManager.Buildings[houseEntity.Id].BuildRate = 1f;
                // 체력 떨어졌을 때 light 제거, 디스폰되는지 확인

                var cameraPosition = houseEntity.transform.position;
                cameraPosition.z = Camera.main.transform.position.z;
                Camera.main.transform.position = cameraPosition;
            }

            var passablePositions = _world.GetPassablePositions(Vector2Int.one);
            // for (var i = 0; i < 1; i++)
            // {
            //     if (passablePositions.Count == 0) break;
            //     var position = SamplePassablePosition(passablePositions);
            //     if (_world.TrySpawn(housePosition + Vector2Int.right * 5, _treeData, out var entity)) {}
            // }

            // for (var i = 0; i < 1; i++)
            // {
            //     if (passablePositions.Count == 0) break;
            //     var position = SamplePassablePosition(passablePositions);
            //     if (_world.TrySpawn(housePosition + Vector2Int.right * 5, _rockData, out var entity)) {}
            // }

            var enemyPosition = SamplePassablePosition(passablePositions);
            if (_world.TrySpawn(enemyPosition, _enemyData, out var enemyEntity))
            {
                
            }

            isReady = true;

            await Awaitable.EndOfFrameAsync();
            _world.BuildNavMesh();
        }

        private void SpawnCitizenForHouse(Vector3 houseWorldPosition)
        {
            var passablePositions = _world.GetPassablePositions(Vector2Int.one);
            if (passablePositions.Count == 0) return;

            var housePosition = (Vector2)houseWorldPosition;
            var citizenPosition = passablePositions.OrderBy(p => ((Vector2)p - housePosition).sqrMagnitude).First();

            if (!_world.TrySpawn(citizenPosition, _citizenData, out var entity))
                return;

            // 충돌 이벤트 → ObjectSystem 전달
            // citizenAgent.TryGetComponent(out Game.Field.Citizen citizen);
            // citizen.OnInteractionCollided += (collider, field, citizen) =>
            // {
            //     // var other = _objectSystem.GetModel(collider.gameObject);
            //     // if (other != null) _objectSystem.RegisterCollision(citizen, other);
            // };

            // _houseCitizenPairs[house] = citizenAgent;
            // citizenAgent.OnDied += (diedInfo, o) =>
            // {
            //     _houseCitizenPairs.Remove(house);
            //     _world.Fog.RemoveLight(light.Light);
            //     _world.Despawn(o);
            // };
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
            application.Update(Time.deltaTime);

            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!componentManager.HasDespawnTag[i]) continue;
                componentManager.HasDespawnTag[i] = false;
                _world.Despawn(i);
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                _modeSystem.Select<BuildMode, BuildModeParam>(null, new BuildModeParam() { Data = _houseData });
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Escape))
            {
                _modeSystem.Select<SelectMode>();
            }
        }
    }
}
