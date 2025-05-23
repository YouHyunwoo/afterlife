using System;
using UnityEngine;

namespace Afterlife.Controller
{
    public class StageController : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] StageGenerator stageGenerator;
        [SerializeField] ObjectGenerator objectGenerator;
        [SerializeField] MonsterSpawner monsterSpawner;
        [SerializeField] TileInteractionController tileInteractionController;

        [Header("View")]
        [SerializeField] Camera mainCamera;
        [SerializeField] View.Stage stageView;
        public Transform terrainTransform;
        public Transform fieldTransform;

        public TimeHandler TimeHandler;
        public MenuHandler MenuHandler;
        public MissionHandler MissionHandler;
        public SkillHandler SkillHandler;
        // WorldHandler
        // ExperienceHandler
        // InGameMenuHandler
        // MissionHandler
        // CraftingHandler
        // InventoryHandler

        Model.Player player;
        Model.Stage stage;

        public event Action OnStageClearedEvent;
        public event Action OnStageFailedEvent;
        public event Action OnGameClearedEvent;
        public event Action OnGameOverEvent;

        void LateUpdate()
        {
            if (stage == null) { return; }
            if (!stage.Map.Fog.IsDirty) { return; }
            stage.Map.Fog.Update();
        }

        public void SetUp()
        {
            GenerateStage();
            SetUpPlayer();
            GenerateObjects();

            TimeHandler = new(Controller.Instance);
            MenuHandler = new(Controller.Instance);
            MissionHandler = new(Controller.Instance);
            SkillHandler = new(Controller.Instance);
            MissionHandler.OnMissionSuccessEvent += OnMissionSuccessed;
            MissionHandler.OnMissionFailedEvent += OnMissionFailed;
            monsterSpawner.OnMonsterSpawned += MissionHandler.OnMonsterSpawned;
            TimeHandler.SetUp();
            MenuHandler.SetUp();
            MissionHandler.SetUp();
            SkillHandler.SetUp();
            monsterSpawner.SetUp();
            tileInteractionController.SetUp();

            enabled = true;
        }

        void GenerateStage()
        {
            var game = Controller.Instance.Game;

            var stageData = game.Data.StageDataArray[game.CurrentStageIndex];
            stage = game.Stage = stageGenerator.Generate(stageData);
        }

        void SetUpPlayer()
        {
            var game = Controller.Instance.Game;

            player = game.Player;
            player.OnExperienceChanged += OnExperienceChanged;
            stage.Map.Fog.Lights.Add(player.Light);

            var map = stage.Map;
            mainCamera.transform.position = new Vector3(map.Size.x / 2f, map.Size.y / 2f, -10f);
        }

        void OnExperienceChanged(float experience)
        {
            stageView.SetExperience(experience);
        }

        void GenerateObjects()
        {
            var fieldData = stage.Data.MapData.FieldData;
            var mapSize = stage.Data.MapData.Size;
            var map = stage.Map;
            var field = map.Field;
            GenerateVillages(fieldData, mapSize, field, map);
            GenerateEnvironments(fieldData, mapSize, field, map);
            GenerateMonsters(fieldData, mapSize, field, map);
        }

        void VerifyFieldData(Data.Field fieldData, Vector2Int mapSize)
        {
            if (fieldData == null) { throw new System.Exception("Field data cannot be null."); }
            if (fieldData.VillagePrefab == null) { throw new System.Exception("Village prefab cannot be null."); }
            if (fieldData.ResourceObjectGroups == null) { throw new System.Exception("Resource object groups cannot be null or empty."); }

            var villageCount = fieldData.VillageCount;
            var resourceObjectCount = 0;
            foreach (var resourceObjectGroup in fieldData.ResourceObjectGroups)
            {
                if (resourceObjectGroup == null) { continue; }
                resourceObjectCount += resourceObjectGroup.Count;
            }
            var initialMonsterCount = 0;
            var totalObjectCount = villageCount + resourceObjectCount + initialMonsterCount;

            var mapVolume = mapSize.x * mapSize.y;
            if (totalObjectCount > mapVolume)
            {
                throw new System.Exception($"Total object count exceeds map volume: {villageCount + resourceObjectCount} > {mapVolume}");
            }
        }

        void GenerateVillages(Data.Field fieldData, Vector2Int mapSize, Model.Field field, Model.Map map)
        {
            var villageCount = fieldData.VillageCount;
            var villagePrefab = fieldData.VillagePrefab;

            if (villageCount <= 0) { throw new System.Exception("Village count must be greater than 0."); }
            if (villagePrefab == null) { throw new System.Exception("Village prefab cannot be null."); }
            if (field.GetEmptyCount() < villageCount) { throw new System.Exception($"Not enough space to generate {villageCount} villages."); }

            for (int i = 0; i < villageCount; i++)
            {
                var location = new Vector2Int(UnityEngine.Random.Range(0, mapSize.x), UnityEngine.Random.Range(0, mapSize.y));
                if (field.Has(location)) { i--; continue; }

                var fieldObject = objectGenerator.Generate(villagePrefab, location);
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

                stage.Map.Fog.AddLight(villageLight);

                field.Set(location, fieldObject.transform);
            }
        }

        void GenerateEnvironments(Data.Field fieldData, Vector2Int mapSize, Model.Field field, Model.Map map)
        {
            foreach (var resourceObjectGroup in fieldData.ResourceObjectGroups)
            {
                for (int i = 0; i < resourceObjectGroup.Count; i++)
                {
                    var location = new Vector2Int(UnityEngine.Random.Range(0, mapSize.x), UnityEngine.Random.Range(0, mapSize.y));
                    if (field.Has(location)) { continue; }

                    var fieldObject = objectGenerator.Generate(resourceObjectGroup.Prefab, location);
                    if (!fieldObject.TryGetComponent(out View.Resource resource))
                    {
                        Debug.LogError($"Object {resourceObjectGroup.Prefab.name} does not have a Resource component.");
                        continue;
                    }

                    resource.Health = UnityEngine.Random.Range(resourceObjectGroup.MinHealth, resourceObjectGroup.MaxHealth + 1);
                    resource.Type = resourceObjectGroup.Name;
                    resource.Amount = UnityEngine.Random.Range(resourceObjectGroup.MinAmount, resourceObjectGroup.MaxAmount + 1);

                    field.Set(location, fieldObject.transform);
                }
            }
        }

        void GenerateMonsters(Data.Field fieldData, Vector2Int mapSize, Model.Field field, Model.Map map)
        {
            foreach (var monsterObjectGroup in fieldData.MonsterObjectGroups)
            {
                for (int i = 0; i < monsterObjectGroup.Count; i++)
                {
                    var location = new Vector2Int(UnityEngine.Random.Range(0, mapSize.x), UnityEngine.Random.Range(0, mapSize.y));
                    if (field.Has(location)) { continue; }

                    var fieldObject = objectGenerator.Generate(monsterObjectGroup.Prefab, location);
                    if (!fieldObject.transform.TryGetComponent(out View.Monster monster))
                    {
                        Debug.LogError($"Object {monsterObjectGroup.Prefab.name} does not have a Monster component.");
                        continue;
                    }

                    field.Set(location, fieldObject.transform);
                }
            }
        }

        void OnMissionSuccessed()
        {
            var game = Controller.Instance.Game;

            game.CurrentStageIndex++;
            if (game.CurrentStageIndex >= game.TotalStageCount)
            {
                OnGameClearedEvent?.Invoke();
            }
            else
            {
                OnStageClearedEvent?.Invoke();
            }
        }

        void OnMissionFailed()
        {
            var game = Controller.Instance.Game;

            game.Lifes--;
            if (game.Lifes <= 0)
            {
                game.Lifes = 0;
                OnGameOverEvent?.Invoke();
            }
            else
            {
                OnStageFailedEvent?.Invoke();
            }
        }

        public void TearDown()
        {
            enabled = false;

            tileInteractionController.TearDown();
            monsterSpawner.OnMonsterSpawned -= MissionHandler.OnMonsterSpawned;
            monsterSpawner.TearDown();
            SkillHandler.TearDown();
            SkillHandler = null;
            MissionHandler.TearDown();
            MissionHandler = null;
            MenuHandler.TearDown();
            MenuHandler = null;
            TimeHandler.TearDown();
            TimeHandler = null;

            player.OnExperienceChanged -= OnExperienceChanged;
            player.Light.IsActive = false;
            player = null;
            stage = null;
            stageGenerator.Clear();
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;

            TimeHandler.Update(deltaTime);
        }
    }
}