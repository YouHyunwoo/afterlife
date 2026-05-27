using System;
using System.Collections.Generic;
using Afterlife.GameSystem.Stage;
using Afterlife.GameSystem.Stage.Field;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.Core
{
    public class StageManager : ManagerBase
    {
        [Header("Player")]
        [SerializeField] PlayerModeSystem playerModeSystem;
        [SerializeField] InputSystem inputSystem;

        [Header("Terrain")]
        [SerializeField] Transform terrainTransform;
        [SerializeField] Tilemap terrainTilemap;
        [SerializeField] TileBase dirtTile;
        [SerializeField] TileBase waterTile;

        [Header("Fog")]
        [SerializeField] Tilemap fogTilemap;
        [SerializeField] TileBase fogTile;

        [Header("Field")]
        [SerializeField] Transform fieldTransform;
        public FieldObjectSystem fieldObjectSystem;
        [SerializeField] ObjectSpawnSystem objectSpawnSystem;
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

        StageObjectBuildSystem stageObjectBuildSystem;
        StageEventBindingSystem stageEventBindingSystem;
        StagePlayerBindingSystem stagePlayerBindingSystem;
        readonly List<Action> stageCleanupActions = new();
        bool isDispositionRequested;

        public override void SetUp()
        {
            stageObjectBuildSystem = new StageObjectBuildSystem(
                terrainTilemap,
                dirtTile,
                waterTile,
                fogTilemap,
                fogTile,
                fieldObjectSystem);
            stageEventBindingSystem = new StageEventBindingSystem();
            stagePlayerBindingSystem = new StagePlayerBindingSystem();

            enabled = false;
        }

        public override void TearDown()
        {
            DisposeStage();
        }

        public void StartStage()
        {
            if (Stage != null || stageCleanupActions.Count > 0)
            {
                DisposeStage();
            }

            try
            {
                CreateStage();
                MapGenerator.Generate(Stage);

                var objectStage = Stage;
                RegisterStageCleanup(() => stageObjectBuildSystem.Clear(objectStage));

                Model.Stage playerBoundStage = null;
                RegisterStageCleanup(() =>
                {
                    if (playerBoundStage == null) { return; }

                    stagePlayerBindingSystem.Unbind(playerBoundStage);
                });

                RegisterStageCleanup(() => stageEventBindingSystem.Unbind(
                    timeSystem,
                    missionSystem,
                    objectSpawnSystem,
                    fieldObjectSystem,
                    SuccessStage,
                    FailStage));
                stageEventBindingSystem.Bind(
                    timeSystem,
                    missionSystem,
                    objectSpawnSystem,
                    fieldObjectSystem,
                    SuccessStage,
                    FailStage);

                SetUpStageSystem(timeSystem);
                SetUpStageSystem(tileInteractionSystem);
                SetUpStageSystem(missionSystem);
                SetUpStageSystem(fieldObjectSystem);
                SetUpStageSystem(objectSpawnSystem);
                SetUpStageSystem(skillSystem);
                SetUpStageSystem(itemCollectSystem);
                SetUpStageSystem(itemUsageSystem);
                SetUpStageSystem(craftSystem);
                SetUpStageSystem(rewardSystem);
                SetUpStageSystem(equipmentSystem);
                SetUpStageSystem(cameraSystem);
                SetUpStageSystem(playerModeSystem);
                SetUpStageSystem(constructionSystem);
                SetUpStageSystem(tileIndicationSystem);
                SetUpStageSystem(inputSystem);

                stageObjectBuildSystem.Build(Stage, out var pivot);

                playerBoundStage = Stage;
                stagePlayerBindingSystem.Bind(playerBoundStage);

                Stage.Map.Fog.Invalidate();
                cameraSystem.SetCameraPosition(pivot);

                isDispositionRequested = false;
                enabled = true;
            }
            catch
            {
                DisposeStage();
                throw;
            }
        }

        void CreateStage()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            var stageData = game.Data.StageDataArray[game.CurrentStageIndex];
            Stage = game.Stage = StageFactory.Create(stageData);
        }

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
                ServiceLocator.Get<UIManager>().Fade(() =>
                {
                    ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Main);
                });
            }
        }

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
                ServiceLocator.Get<UIManager>().Fade(() =>
                {
                    ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Main);
                });
            }
        }

        public void EndStage() => RequestDispose();

        void Update()
        {
            inputSystem.UpdateSystem();
            cameraSystem.UpdateSystem();
            timeSystem.UpdateSystem();
            objectSpawnSystem.UpdateSystem();
            constructionSystem.UpdateSystem();
        }

        void LateUpdate()
        {
            if (isDispositionRequested)
            {
                DisposeStage();
            }
            else if (Stage?.Map?.Fog != null)
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
            if (Stage == null && stageCleanupActions.Count == 0)
            {
                enabled = false;
                isDispositionRequested = false;
                return;
            }

            enabled = false;
            isDispositionRequested = false;

            ExecuteStageCleanupActions();
            DeleteStageSafely();
        }

        void RegisterStageCleanup(Action cleanupAction)
        {
            stageCleanupActions.Add(cleanupAction);
        }

        void SetUpStageSystem(SystemBase system)
        {
            RegisterStageCleanup(system.TearDown);
            system.SetUp();
        }

        void ExecuteStageCleanupActions()
        {
            for (int i = stageCleanupActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    stageCleanupActions[i]?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            stageCleanupActions.Clear();
        }

        void DeleteStageSafely()
        {
            try
            {
                DeleteStage();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        void DeleteStage()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            Stage = game.Stage = null;
        }
    }
}
