using UnityEngine;

namespace Afterlife.Core
{
    /// <summary>
    /// 게임을 관리하는 GameManager
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public Model.Game Game;

        public void StartGame()
        {
            ServiceLocator.Get<UIManager>().FadeTransition(() =>
            {
                CreateGame();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Main);
            });
        }

        void CreateGame()
        {
            // 게임 생성 로직
            // 예: 플레이어, 스테이지, 오브젝트 초기화 등
            var gameData = ServiceLocator.Get<DataManager>().GameData;
            Game = new Model.Game
            {
                Data = gameData,
                Upgrade = new Model.Upgrade(),
                Reward = new Model.Reward(),
                Lives = gameData.Lifes,
                Player = new Model.Player
                {
                    Experience = gameData.Experience,
                    AttackPower = 1f,
                    AttackSpeed = 1f,
                    AttackRange = 1f,
                    AttackCount = 1f,
                    CriticalRate = 0.0f,
                    CriticalDamageMultiplier = 1.2f,
                    RecoveryPower = 1f,
                    RewardSelectionCount = 3,
                    Light = new Model.Light
                    {
                        Location = Vector2Int.zero,
                        Intensity = 3f,
                        Range = 5f,
                        IsActive = false,
                    },
                    Skills = new(),
                },
                CurrentStageIndex = 0,
                TotalStageCount = gameData.StageDataArray.Length,
            };

            Debug.Log("[System] 게임 생성");
        }

        void SaveGame()
        {
            // 게임 저장 로직
            // 예: 플레이어 상태, 스테이지 진행 상황 등을 저장
            Debug.Log("[System] 게임 저장");
        }

        public void QuitGame()
        {
            ServiceLocator.Get<UIManager>().FadeTransition(() =>
            {
                // SaveGame();
                DeleteGame();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Title);
            });
        }

        public void SucceedGame()
        {
            ServiceLocator.Get<UIManager>().FadeTransition(() =>
            {
                DeleteGame();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Demo);
            });
        }

        public void FailGame()
        {
            ServiceLocator.Get<UIManager>().FadeTransition(() =>
            {
                DeleteGame();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.GameOver);
            });
        }

        void DeleteGame()
        {
            // 게임 종료 로직
            // 예: 플레이어 상태 초기화, 스테이지 리셋 등
            ServiceLocator.Get<UIManager>().MainController.ResetView();

            Game = null;

            Debug.Log("[System] 게임 삭제");
        }

        public void StartStage()
        {
            ServiceLocator.Get<UIManager>().FadeTransition(() =>
            {
                CreateStage();
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.InGame);
            });
        }

        void CreateStage()
        {
            // 스테이지 생성 로직
            // 예: 스테이지 데이터 초기화, 몬스터 생성 등
            ServiceLocator.Get<StageManager>().StartStage();

            Debug.Log("[System] 스테이지 생성");
        }
    }
}
