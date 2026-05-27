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
        [SerializeField] Transform fogTransform;
        [SerializeField] Transform fogPrefab;

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
        bool isDispositionRequested;

        public override void SetUp()
        {
            stageObjectBuildSystem = new StageObjectBuildSystem(
                terrainTilemap,
                dirtTile,
                waterTile,
                fogTransform,
                fogPrefab,
                fieldObjectSystem);
            stageEventBindingSystem = new StageEventBindingSystem();
            stagePlayerBindingSystem = new StagePlayerBindingSystem();

            enabled = false;
        }

        public override void TearDown()
        {
            enabled = false;
        }

        public void StartStage()
        {
            CreateStage();
            MapGenerator.Generate(Stage);

            stageEventBindingSystem.Bind(
                timeSystem,
                missionSystem,
                objectSpawnSystem,
                fieldObjectSystem,
                SuccessStage,
                FailStage);

            timeSystem.SetUp();
            tileInteractionSystem.SetUp();
            missionSystem.SetUp();
            fieldObjectSystem.SetUp();
            objectSpawnSystem.SetUp();
            skillSystem.SetUp();
            itemCollectSystem.SetUp();
            itemUsageSystem.SetUp();
            craftSystem.SetUp();
            rewardSystem.SetUp();
            equipmentSystem.SetUp();
            cameraSystem.SetUp();
            playerModeSystem.SetUp();
            constructionSystem.SetUp();
            tileIndicationSystem.SetUp();
            inputSystem.SetUp();

            stageObjectBuildSystem.Build(Stage, out var pivot);
            stagePlayerBindingSystem.Bind(Stage);
            Stage.Map.Fog.Invalidate();
            cameraSystem.SetCameraPosition(pivot);

            isDispositionRequested = false;
            enabled = true;
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

            inputSystem.TearDown();
            tileIndicationSystem.TearDown();
            constructionSystem.TearDown();
            playerModeSystem.TearDown();
            cameraSystem.TearDown();
            equipmentSystem.TearDown();
            rewardSystem.TearDown();
            craftSystem.TearDown();
            itemUsageSystem.TearDown();
            itemCollectSystem.TearDown();
            skillSystem.TearDown();
            objectSpawnSystem.TearDown();
            fieldObjectSystem.TearDown();
            missionSystem.TearDown();
            tileInteractionSystem.TearDown();
            timeSystem.TearDown();

            stageEventBindingSystem.Unbind(
                timeSystem,
                missionSystem,
                objectSpawnSystem,
                fieldObjectSystem,
                SuccessStage,
                FailStage);

            stagePlayerBindingSystem.Unbind(Stage);
            stageObjectBuildSystem.Clear(Stage);
            DeleteStage();
        }

        void DeleteStage()
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            Stage = game.Stage = null;
        }
    }
}
