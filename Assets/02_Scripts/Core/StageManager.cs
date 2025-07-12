using Afterlife.GameSystem.Stage;
using Afterlife.GameSystem.Stage.Field;
using UnityEngine;

namespace Afterlife.Core
{
    public class StageManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] PlayerModeSystem playerModeSystem;

        [Header("Terrain")]
        [SerializeField] Transform terrainTransform;

        [Header("Fog")]
        [SerializeField] Transform fogTransform;
        [SerializeField] Transform fogPrefab;

        [Header("Field")]
        [SerializeField] Transform fieldTransform;
        public FieldObjectSystem fieldObjectSystem;
        [SerializeField] ObjectSpawnSystem objectSpawnSystem;
        [SerializeField] EnvironmentSpawnSystem environmentSpawnSystem;
        [SerializeField] ConstructionSystem constructionSystem;

        [Header("Time")]
        [SerializeField] TimeSystem timeSystem;
        [SerializeField] RewardSystem rewardSystem;

        [Header("Tile")]
        [SerializeField] TileInteractionSystem tileInteractionSystem;
        [SerializeField] TileIndicationSystem tileIndicationSystem;

        [Header("Mission")]
        [SerializeField] MissionSystem missionSystem;

        [Header("Skill")]
        [SerializeField] SkillSystem skillSystem;

        [Header("Item")]
        public ItemCollectSystem itemCollectSystem;
        [SerializeField] ItemUsageSystem itemUsageSystem;
        [SerializeField] EquipmentSystem equipmentSystem;
        [SerializeField] CraftSystem craftSystem;

        [Header("Camera")]
        [SerializeField] CameraSystem cameraSystem;

        public Model.Stage Stage;

        bool isDispositionRequested;

        public void StartStage()
        {
            CreateStage();
            GenerateMap();

            timeSystem.OnDayChangedEvent += missionSystem.OnDayChanged;
            timeSystem.OnDayChangedEvent += objectSpawnSystem.OnDayChanged;
            // timeSystem.OnDayChangedEvent += rewardSystem.OnDayChanged;
            missionSystem.OnMissionSuccessEvent += OnMissionSuccessed;
            missionSystem.OnMissionFailedEvent += OnMissionFailed;
            fieldObjectSystem.OnObjectSpawnedEvent += missionSystem.OnObjectSpawned;
            objectSpawnSystem.OnObjectSpawnedEvent += missionSystem.OnObjectSpawned;

            timeSystem.SetUp();
            tileInteractionSystem.SetUp();
            missionSystem.SetUp();
            fieldObjectSystem.SetUp();
            objectSpawnSystem.SetUp();
            skillSystem.SetUp();
            environmentSpawnSystem.SetUp();
            itemCollectSystem.SetUp();
            itemUsageSystem.SetUp();
            craftSystem.SetUp();
            rewardSystem.SetUp();
            equipmentSystem.SetUp();
            cameraSystem.SetUp();
            playerModeSystem.SetUp();
            constructionSystem.SetUp();
            tileIndicationSystem.SetUp();

            CreateObjectsForStage();
            SetUpPlayer();
            Stage.Map.Fog.Invalidate();

            isDispositionRequested = false;
            enabled = true;
        }

        void CreateStage()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            var stageData = game.Data.StageDataArray[game.CurrentStageIndex];
            Stage = game.Stage = StageFactory.Create(stageData);
        }

        void GenerateMap()
        {
            var mapSize = Stage.Map.Size;
            var mapData = Stage.Data.MapData;
            var terrain = Stage.Map.Terrain;

            var fnl = new FastNoiseLite();
            fnl.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            fnl.SetFractalType(FastNoiseLite.FractalType.FBm);
            fnl.SetFrequency(mapData.GenerationFrequency);
            fnl.SetFractalOctaves(mapData.GenerationOctaves);
            fnl.SetSeed(Random.Range(0, int.MaxValue));

            var radialMask = new float[mapSize.x * mapSize.y];
            var index = 0;
            for (var y = 0; y < mapSize.y; y++)
            {
                for (var x = 0; x < mapSize.x; x++)
                {
                    var dx = x - mapSize.x / 2f;
                    var dy = y - mapSize.y / 2f;
                    var value = mapData.GenerationRadius - Mathf.Sqrt(dx * dx + dy * dy);
                    radialMask[index++] = Mathf.Clamp01(value / mapData.GenerationRadius);
                }
            }

            var heightMap = new float[mapSize.x * mapSize.y];
            index = 0;
            for (var y = 0; y < mapSize.y; y++)
            {
                for (var x = 0; x < mapSize.x; x++)
                {
                    var noiseValue = fnl.GetNoise(x, y);
                    var height = (noiseValue + 1) * 0.5f;
                    height = Mathf.Pow(height, mapData.GenerationScale);
                    height *= radialMask[index];
                    height = height > mapData.GenerationHeightThreshold ? 1f : 0f;
                    heightMap[index++] = height;
                }
            }

            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    var isLand = heightMap[y * mapSize.x + x] > 0;
                    var tileIndex = isLand ? 1 : 3;
                    terrain.TerrainGrid[x, y] = tileIndex;
                    terrain.PassableGrid[x, y] = isLand;
                }
            }
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
            GenerateVillages(fieldData);
            GenerateEnvironments(fieldData);
        }

        void GenerateVillages(Data.Field fieldData)
        {
            var padding = 2;
            var villageCount = fieldData.VillageCount;
            var villagePrefab = fieldData.VillagePrefab;

            if (villageCount <= 0) { throw new System.Exception("Village count must be greater than 0."); }
            if (villagePrefab == null) { throw new System.Exception("Village prefab cannot be null."); }
            if (Stage.Map.Field.GetEmptyCount() < villageCount) { throw new System.Exception($"Not enough space to generate {villageCount} villages."); }

            var mapSize = Stage.Map.Size;

            for (int i = 0; i < villageCount; i++)
            {
                var x = Random.Range(padding, mapSize.x - padding);
                var y = Random.Range(padding, mapSize.y - padding);
                var location = new Vector2Int(x, y);
                if (!Stage.Map.IsAvailable(location)) { i--; continue; }

                var fieldObject = fieldObjectSystem.Spawn(villagePrefab, location);
                if (!fieldObject.TryGetComponent(out View.Object village))
                {
                    Debug.LogError($"Village prefab {villagePrefab.name} does not have a Village component.");
                    continue;
                }
            }
        }

        void GenerateEnvironments(Data.Field fieldData)
        {
            var mapSize = Stage.Map.Size;

            foreach (var resourceObjectGroup in fieldData.ResourceObjectGroups)
            {
                for (int i = 0; i < resourceObjectGroup.Count; i++)
                {
                    var x = Random.Range(0, mapSize.x);
                    var y = Random.Range(0, mapSize.y);
                    var location = new Vector2Int(x, y);
                    if (!Stage.Map.IsAvailable(location)) { continue; }

                    var fieldObject = fieldObjectSystem.Spawn(resourceObjectGroup.Prefab, location);
                    if (!fieldObject.TryGetComponent(out View.Resource resource))
                    {
                        Debug.LogError($"Object {resourceObjectGroup.Prefab.name} does not have a Resource component.");
                        continue;
                    }

                    resource.Value = Random.Range(resourceObjectGroup.MinHealth, resourceObjectGroup.MaxHealth + 1);
                    resource.Type = resourceObjectGroup.Name;
                    resource.Amount = Random.Range(resourceObjectGroup.MinAmount, resourceObjectGroup.MaxAmount + 1);
                }
            }
        }

        void SetUpPlayer()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            var player = game.Player;

            player.OnExperienceChanged += OnExperienceChanged;
            Stage.Map.Fog.Lights.Add(player.Light);
        }

        void OnExperienceChanged(float experience)
        {
            var stageScreen = ServiceLocator.Get<UIManager>().InGameScreen as UI.Stage.Screen;
            stageScreen.ExperienceView.SetAmount(experience);
        }

        void OnMissionSuccessed() => SuccessStage();

        public void SuccessStage()
        {
            EndStage();

            var game = ServiceLocator.Get<GameManager>().Game;

            game.CurrentStageIndex++;
            if (game.CurrentStageIndex >= game.TotalStageCount)
            {
                ServiceLocator.Get<GameManager>().SucceedGame();
            }
            else
            {
                ServiceLocator.Get<UIManager>().FadeTransition(() =>
                {
                    ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Main);
                });
            }
        }

        void OnMissionFailed() => FailStage();

        public void FailStage()
        {
            EndStage();

            var game = ServiceLocator.Get<GameManager>().Game;

            game.Lives--;
            if (game.Lives <= 0)
            {
                game.Lives = 0;
                ServiceLocator.Get<GameManager>().FailGame();
            }
            else
            {
                ServiceLocator.Get<UIManager>().FadeTransition(() =>
                {
                    ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Main);
                });
            }
        }

        public void EndStage() => RequestDispose();

        void LateUpdate()
        {
            if (isDispositionRequested)
            {
                DisposeStage();
            }
            else
            {
                Stage.Map.Fog.Update();
            }
        }

        void RequestDispose()
        {
            isDispositionRequested = true;
        }

        void DisposeStage()
        {
            enabled = false;
            isDispositionRequested = false;

            tileIndicationSystem.TearDown();
            constructionSystem.TearDown();
            playerModeSystem.TearDown();
            cameraSystem.TearDown();
            equipmentSystem.TearDown();
            rewardSystem.TearDown();
            craftSystem.TearDown();
            itemUsageSystem.TearDown();
            itemCollectSystem.TearDown();
            environmentSpawnSystem.TearDown();
            skillSystem.TearDown();
            objectSpawnSystem.TearDown();
            fieldObjectSystem.TearDown();
            missionSystem.TearDown();
            tileInteractionSystem.TearDown();
            timeSystem.TearDown();

            timeSystem.OnDayChangedEvent -= missionSystem.OnDayChanged;
            timeSystem.OnDayChangedEvent -= objectSpawnSystem.OnDayChanged;
            timeSystem.OnDayChangedEvent -= rewardSystem.OnDayChanged;
            missionSystem.OnMissionSuccessEvent -= OnMissionSuccessed;
            missionSystem.OnMissionFailedEvent -= OnMissionFailed;
            fieldObjectSystem.OnObjectSpawnedEvent -= missionSystem.OnObjectSpawned;
            objectSpawnSystem.OnObjectSpawnedEvent -= missionSystem.OnObjectSpawned;

            TearDownPlayer();
            DeleteObjectsForStage();
            DeleteStage();
        }

        void TearDownPlayer()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            var player = game.Player;

            player.OnExperienceChanged -= OnExperienceChanged;
            Stage.Map.Fog.Lights.Remove(player.Light);
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