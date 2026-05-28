using UnityEngine;

namespace Afterlife.Core
{
    public class GameManager : ManagerBase
    {
        public Model.Game Game;

        public void StartGame()
        {
            ServiceLocator.Get<UIManager>().Fade(() =>
            {
                CreateGame();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Main);
            });
        }

        void CreateGame()
        {
            var gameData = ServiceLocator.Get<DataManager>().GameData;
            Game = new Model.Game
            {
                Data = gameData,
                IsMainTutorialActive = true,
                IsStageTutorialActive = true,
                Upgrade = new Model.Upgrade(),
                Artifact = new Model.Artifact(),
                Reward = new Model.Reward(),
                Lives = gameData.Lifes,
                Player = new Model.Player
                {
                    Experience = gameData.Experience,
                    Level = 1,
                    CurrentLevelExperience = 0f,
                    RequiredLevelExperience = GetInitialLevelExperience(gameData),
                    LevelExperienceGrowth = GetLevelExperienceGrowth(gameData),
                    AttackPower = 1f,
                    AttackSpeed = 1f,
                    AttackRange = 1f,
                    AttackCount = 1f,
                    CriticalRate = 0.0f,
                    CriticalDamageMultiplier = 2f,
                    RecoveryPower = 1f,
                    MaxEquipmentCount = 6,
                    RewardSelectionCount = 3,
                    Light = new Model.Light
                    {
                        Location = Vector2Int.zero,
                        Intensity = 1f,
                        Range = 3f,
                        IsActive = false,
                    },
                    Skills = new(),
                },
                CurrentStageIndex = 0,
                TotalStageCount = gameData.StageDataArray.Length,
            };

            InitializeRecipeUnlocks(Game.Player);

            Debug.Log("[System] Game created");
        }

        float GetInitialLevelExperience(Data.Game gameData)
        {
            return gameData.BaseLevelExperience > 0f ? gameData.BaseLevelExperience : 100f;
        }

        float GetLevelExperienceGrowth(Data.Game gameData)
        {
            return gameData.LevelExperienceGrowth > 0f ? gameData.LevelExperienceGrowth : 1.2f;
        }

        void InitializeRecipeUnlocks(Model.Player player)
        {
            var dataManager = ServiceLocator.Get<DataManager>();
            var recipeUnlocks = dataManager.RecipeUnlockDataArray;
            if (recipeUnlocks == null || recipeUnlocks.Length == 0)
            {
                foreach (var itemId in dataManager.CraftableItemIds)
                {
                    player.UnlockedRecipeIds.Add(itemId);
                }

                return;
            }

            foreach (var recipeUnlock in recipeUnlocks)
            {
                if (recipeUnlock == null) { continue; }
                var hasPrerequisites = recipeUnlock.PrerequisiteRecipeIds != null &&
                    recipeUnlock.PrerequisiteRecipeIds.Length > 0;
                if (recipeUnlock.Cost <= 0 && !hasPrerequisites)
                {
                    player.UnlockedRecipeIds.Add(recipeUnlock.Id);
                }
            }
        }

        void SaveGame()
        {
            Debug.Log("[System] Game saved");
        }

        public void QuitGame()
        {
            ServiceLocator.Get<UIManager>().Fade(() =>
            {
                // SaveGame();
                DeleteGame();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Title);
            });
        }

        public void SucceedGame()
        {
            ServiceLocator.Get<UIManager>().Fade(() =>
            {
                DeleteGame();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Demo);
            });
        }

        public void FailGame()
        {
            ServiceLocator.Get<UIManager>().Fade(() =>
            {
                DeleteGame();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.GameOver);
            });
        }

        void DeleteGame()
        {
            ServiceLocator.Get<UI.Main.Controller>().ResetView();

            Game = null;

            Debug.Log("[System] Game deleted");
        }

        public void StartStage()
        {
            ServiceLocator.Get<UIManager>().Fade(() =>
            {
                CreateStage();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.InGame);
            });
        }

        void CreateStage()
        {
            ServiceLocator.Get<StageManager>().StartStage();

            Debug.Log("[System] Stage created");
        }
    }
}
