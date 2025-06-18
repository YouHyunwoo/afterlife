using Afterlife.GameSystem.Stage;
using Afterlife.GameSystem.Stage.Field;
using UnityEngine;

namespace Afterlife.Core
{
    public class StageManager : MonoBehaviour
    {
        [Header("Terrain")]
        [SerializeField] Transform terrainTransform;

        [Header("Fog")]
        [SerializeField] Transform fogTransform;
        [SerializeField] Transform fogPrefab;
        [SerializeField] FogSystem fogSystem;

        [Header("Field")]
        [SerializeField] Transform fieldTransform;
        [SerializeField] FieldObjectSpawner fieldObjectSpawner;
        [SerializeField] MonsterSpawnSystem monsterSpawnSystem;
        [SerializeField] EnvironmentSpawnSystem environmentSpawnSystem;

        [Header("Time")]
        [SerializeField] TimeSystem timeSystem;

        [Header("Tile Interaction")]
        [SerializeField] TileInteractionSystem tileInteractionSystem;

        [Header("Mission")]
        [SerializeField] MissionSystem missionSystem;

        [Header("Skill")]
        [SerializeField] SkillSystem skillSystem;

        [Header("Camera")]
        [SerializeField] Camera mainCamera;

        public Model.Stage Stage;

        public void StartStage()
        {
            CreateStage();
            CreateObjectsForStage();
            SetUpPlayer();

            missionSystem.OnMissionSuccessEvent += OnMissionSuccessed;
            missionSystem.OnMissionFailedEvent += OnMissionFailed;
            monsterSpawnSystem.OnMonsterSpawned += missionSystem.OnMonsterSpawned;

            ServiceLocator.Register(timeSystem);
            ServiceLocator.Register(tileInteractionSystem);
            ServiceLocator.Register(fogSystem);
            ServiceLocator.Register(missionSystem);
            ServiceLocator.Register(monsterSpawnSystem);
            ServiceLocator.Register(skillSystem);
            ServiceLocator.Register(environmentSpawnSystem);

            timeSystem.SetUp();
            tileInteractionSystem.SetUp();
            fogSystem.SetUp();
            missionSystem.SetUp();
            monsterSpawnSystem.SetUp();
            skillSystem.SetUp();
            environmentSpawnSystem.SetUp();

            Stage.Map.Fog.Update();
        }

        void OnMissionSuccessed()
        {
            EndStage();

            var game = ServiceLocator.Get<GameManager>().Game;

            game.CurrentStageIndex++;
            if (game.CurrentStageIndex >= game.TotalStageCount)
            {
                ServiceLocator.Get<GameManager>().ChangeState(GameState.Clear);
            }
            else
            {

                ServiceLocator.Get<GameManager>().ChangeState(GameState.Main);
            }
        }

        void OnMissionFailed()
        {
            EndStage();

            var game = ServiceLocator.Get<GameManager>().Game;

            game.Lifes--;
            if (game.Lifes <= 0)
            {
                game.Lifes = 0;
                ServiceLocator.Get<GameManager>().ChangeState(GameState.GameOver);
            }
            else
            {
                ServiceLocator.Get<GameManager>().ChangeState(GameState.Main);
            }
        }

        void CreateStage()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            var stageData = game.Data.StageDataArray[game.CurrentStageIndex];
            Stage = game.Stage = StageFactory.Create(stageData);
        }

        void CreateObjectsForStage()
        {
            CreateTerrainObjects();
            CreateFogObjects();
            CreateFieldObjects();
        }

        void CreateTerrainObjects()
        {
            var mapSize = Stage.Map.Size;
            var terrain = Stage.Map.Terrain;
            var terrainIndexGrid = terrain.TerrainGrid;
            var terrainTilePrefabs = terrain.Data.TilePrefabs;
            var terrainTransformGrid = terrain.TransformGrid;

            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    var tileIndex = terrainIndexGrid[x, y];
                    var tilePrefab = terrainTilePrefabs[tileIndex];
                    var terrainTileTransform = Instantiate(tilePrefab, new Vector3(x, y), Quaternion.identity, terrainTransform).transform;
                    terrainTileTransform.name = $"{tilePrefab.name} ({x}, {y})";
                    terrainTransformGrid[x, y] = terrainTileTransform;
                }
            }
        }

        void CreateFogObjects()
        {
            var mapSize = Stage.Map.Size;
            var fog = Stage.Map.Fog;

            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    var fogTile = Instantiate(fogPrefab, new Vector3(x, y), Quaternion.identity, fogTransform);
                    fogTile.name = $"FogTile ({x}, {y})";
                    fog.TransformGrid[x, y] = fogTile;
                    fog.SpriteRendererGrid[x, y] = fogTile.GetComponentInChildren<SpriteRenderer>();
                }
            }
        }

        void CreateFieldObjects()
        {
            var fieldData = Stage.Data.MapData.FieldData;
            var mapSize = Stage.Data.MapData.Size;
            var map = Stage.Map;
            var field = map.Field;
            GenerateVillages(fieldData, mapSize, field, map);
            GenerateEnvironments(fieldData, mapSize, field, map);
            // GenerateMonsters(fieldData, mapSize, field, map);
        }

        void GenerateVillages(Data.Field fieldData, Vector2Int mapSize, Model.Field field, Model.Map map)
        {
            var padding = 2;
            var villageCount = fieldData.VillageCount;
            var villagePrefab = fieldData.VillagePrefab;

            if (villageCount <= 0) { throw new System.Exception("Village count must be greater than 0."); }
            if (villagePrefab == null) { throw new System.Exception("Village prefab cannot be null."); }
            if (field.GetEmptyCount() < villageCount) { throw new System.Exception($"Not enough space to generate {villageCount} villages."); }

            for (int i = 0; i < villageCount; i++)
            {
                var x = Random.Range(padding, mapSize.x - padding);
                var y = Random.Range(padding, mapSize.y - padding);
                var location = new Vector2Int(x, y);
                if (field.Has(location)) { i--; continue; }

                var fieldObject = fieldObjectSpawner.Spawn(villagePrefab, location);
                if (!fieldObject.TryGetComponent(out View.Object village))
                {
                    Debug.LogError($"Village prefab {villagePrefab.name} does not have a Village component.");
                    continue;
                }

                village.Health = 1;

                var villageLight = new Model.Light
                {
                    Location = location,
                    Intensity = 1f,
                    Range = 5f,
                };

                Stage.Map.Fog.AddLight(villageLight);

                field.Set(location, fieldObject.transform);
            }
        }

        void GenerateEnvironments(Data.Field fieldData, Vector2Int mapSize, Model.Field field, Model.Map map)
        {
            foreach (var resourceObjectGroup in fieldData.ResourceObjectGroups)
            {
                for (int i = 0; i < resourceObjectGroup.Count; i++)
                {
                    var x = Random.Range(0, mapSize.x);
                    var y = Random.Range(0, mapSize.y);
                    var location = new Vector2Int(x, y);
                    if (field.Has(location)) { continue; }

                    var fieldObject = fieldObjectSpawner.Spawn(resourceObjectGroup.Prefab, location);
                    if (!fieldObject.TryGetComponent(out View.Resource resource))
                    {
                        Debug.LogError($"Object {resourceObjectGroup.Prefab.name} does not have a Resource component.");
                        continue;
                    }

                    resource.Health = Random.Range(resourceObjectGroup.MinHealth, resourceObjectGroup.MaxHealth + 1);
                    resource.Type = resourceObjectGroup.Name;
                    resource.Amount = Random.Range(resourceObjectGroup.MinAmount, resourceObjectGroup.MaxAmount + 1);

                    field.Set(location, fieldObject.transform);
                }
            }
        }

        void SetUpPlayer()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            var player = game.Player;

            player.OnExperienceChanged += OnExperienceChanged;
            Stage.Map.Fog.Lights.Add(player.Light);

            var mapSize = Stage.Map.Size;
            var targetPosition = new Vector3(mapSize.x / 2f, mapSize.y / 2f, 0f)
            {
                z = mainCamera.transform.position.z
            };
            mainCamera.transform.position = targetPosition;
        }

        void TearDownPlayer()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            var player = game.Player;

            player.OnExperienceChanged -= OnExperienceChanged;
            Stage.Map.Fog.Lights.Remove(player.Light);

            mainCamera.transform.position = new Vector3(0f, 0f, mainCamera.transform.position.z);
        }

        void OnExperienceChanged(float experience)
        {
            var stageScreen = ServiceLocator.Get<UIManager>().InGameScreen as UI.Stage.Screen;
            stageScreen.SetExperience(experience);
        }

        public void EndStage()
        {
            environmentSpawnSystem.TearDown();
            skillSystem.TearDown();
            monsterSpawnSystem.TearDown();
            missionSystem.TearDown();
            fogSystem.TearDown();
            tileInteractionSystem.TearDown();
            timeSystem.TearDown();

            ServiceLocator.Unregister<EnvironmentSpawnSystem>();
            ServiceLocator.Unregister<SkillSystem>();
            ServiceLocator.Unregister<MonsterSpawnSystem>();
            ServiceLocator.Unregister<MissionSystem>();
            ServiceLocator.Unregister<FogSystem>();
            ServiceLocator.Unregister<TileInteractionSystem>();
            ServiceLocator.Unregister<TimeSystem>();

            missionSystem.OnMissionSuccessEvent -= OnMissionSuccessed;
            missionSystem.OnMissionFailedEvent -= OnMissionFailed;
            monsterSpawnSystem.OnMonsterSpawned -= missionSystem.OnMonsterSpawned;

            TearDownPlayer();
            DeleteObjectsForStage();
            DeleteStage();
        }

        void DeleteObjectsForStage()
        {
            DeleteFieldObjects();
            DeleteFogObjects();
            DeleteTerrainObjects();
        }

        void DeleteFieldObjects() => Stage.Map.Field.Dispose();
        void DeleteFogObjects() => Stage.Map.Fog.Dispose();
        void DeleteTerrainObjects() => Stage.Map.Terrain.Dispose();

        void DeleteStage()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            Stage = game.Stage = null;
        }
    }
}