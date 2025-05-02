using System;
using UnityEngine;

namespace Afterlife.Controller
{
    public class StageController : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] StageGenerator stageGenerator;
        [SerializeField] ObjectGenerator objectGenerator;
        [SerializeField] MonsterSpawnController monsterSpawnController;
        [SerializeField] TimeController timeController;
        [SerializeField] TileInteractionController tileInteractionController;

        [Header("View")]
        [SerializeField] Camera mainCamera;
        [SerializeField] View.TerrainTileIndicator terrainTileIndicator;
        [SerializeField] View.Stage stageView;

        public event Action OnGameClearedEvent;
        public event Action OnGameOverEvent;

        public Transform terrainTransform;
        public Transform fieldTransform;

        Model.Player player;
        Model.Stage stage;
        bool isTargetDayReached;

        void LateUpdate()
        {
            if (stage == null) { return; }
            if (!stage.Map.Fog.IsDirty) { return; }
            stage.Map.Fog.Update();
        }

        public void SetUp()
        {
            var game = Controller.Instance.Game;

            player = game.Player;
            player.OnExperienceChanged += OnExperienceChanged;

            var stageData = game.Data.StageDataArray[game.CurrentStageIndex];
            stage = stageGenerator.Generate(stageData);

            stage.Map.Fog.Lights.Add(player.Light);

            objectGenerator.Initialize(stage);
            monsterSpawnController.Initialize(stage);
            monsterSpawnController.OnMonsterSpawned += OnMonsterSpawned;
            timeController.Initialize(stageData.DayDuration, stageData.NightDuration);
            tileInteractionController.Initialize(player, stage);
            tileInteractionController.OnInteractEvent += OnInteractEvent;

            var fieldData = stageData.MapData.FieldData;
            var mapSize = stageData.MapData.Size;
            var map = stage.Map;
            var field = map.Field;
            GenerateVillages(fieldData, mapSize, field, map);
            GenerateEnvironments(fieldData, mapSize, field, map);
            GenerateMonsters(fieldData, mapSize, field, map);

            mainCamera.transform.position = new Vector3(mapSize.x / 2f, mapSize.y / 2f, -10f);
        }

        void OnExperienceChanged(float experience)
        {
            stageView.SetExperience(experience);
        }

        void OnInteractEvent()
        {
            player.TakeExperience(player.AttackPower * player.AttackCount / 10f);
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

                village.Health = 10;
                village.OnDied += OnVillageDied;

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

                    monster.OnDied += OnMonsterDied;

                    field.Set(location, fieldObject.transform);
                }
            }
        }

        void OnMonsterSpawned(View.Monster monster)
        {
            monster.OnDied += OnMonsterDied;
        }

        void OnMonsterDied()
        {
            // TODO: 몬스터가 가진 경험치 획득
            VerifyMissionSuccess();
        }

        void VerifyMissionSuccess()
        {
            // 시간 지나고 몬스터 없으면 미션 성공
            if (!isTargetDayReached) { return; }

            var monsters = fieldTransform.GetComponentsInChildren<View.Monster>();
            if (monsters.Length > 0) { return; }

            Debug.Log("All monsters are dead. Stage cleared!");
            SuccessMission();
        }

        void OnVillageDied()
        {
            VerifyMissionFailure();
        }

        void VerifyMissionFailure()
        {
            // 마을이 다 죽으면 미션 실패
            var villages = fieldTransform.GetComponentsInChildren<View.Village>();
            if (villages.Length > 0) { return; }

            Debug.Log("All villages are dead. Stage failed!");
            FailMission();
        }

        void SuccessMission()
        {
            TearDown();
            Controller.Instance.StageView.Hide();

            var game = Controller.Instance.Game;

            game.CurrentStageIndex++;
            if (game.CurrentStageIndex >= game.TotalStageCount)
            {
                OnGameClearedEvent?.Invoke();
                Controller.Instance.DemoView.Show();
            }
            else
            {
                Controller.Instance.MainView.SetStageProgress(game.CurrentStageIndex, game.TotalStageCount);
                Controller.Instance.MainView.PowerView.ExperienceView.SetExperience(game.Player.Experience);
                Controller.Instance.MainView.Show();
            }
        }

        void FailMission()
        {
            TearDown();
            Controller.Instance.StageView.Hide();

            var game = Controller.Instance.Game;

            game.Lifes--;
            if (game.Lifes <= 0)
            {
                game.Lifes = 0;
                Controller.Instance.GameOverView.Show();
            }
            else
            {
                Controller.Instance.MainView.SetLifes(game.Lifes);
                Controller.Instance.MainView.PowerView.ExperienceView.SetExperience(game.Player.Experience);
                Controller.Instance.MainView.Show();
            }
        }

        public void TearDown()
        {
            isTargetDayReached = false;
            player.OnExperienceChanged -= OnExperienceChanged;
            player.Light.IsActive = false;
            player = null;
            stage = null;
            terrainTileIndicator.gameObject.SetActive(false);
            monsterSpawnController.enabled = false;
            monsterSpawnController.OnMonsterSpawned -= OnMonsterSpawned;
            timeController.enabled = false;
            tileInteractionController.enabled = false;
            tileInteractionController.OnInteractEvent -= OnInteractEvent;
            stageGenerator.Clear();
        }

        public void OnDayChanged(int day)
        {
            if (day >= stage.Data.SpawnIntervalPerDay.Length)
            {
                isTargetDayReached = true;
                VerifyMissionSuccess();
            }
        }

        public void OnTileIndicatorMoved(Vector2Int location)
        {
            if (stage == null) { return; }
            terrainTileIndicator.gameObject.SetActive(true);
            player.Light.IsActive = true;
            player.Light.Location = location;
            stage.Map.Fog.Invalidate();
        }
    }
}