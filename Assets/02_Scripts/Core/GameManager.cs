using DG.Tweening;
using UnityEngine;

namespace Afterlife.Core
{
    public enum GameState
    {
        None,
        Title,
        Main,
        InGame,
        GameClear,
        GameOver,
        Demo,
    }

    /// <summary>
    /// 게임 전체 상태와 흐름을 관리하는 GameManager
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public GameState CurrentState;
        public Model.Game Game;

        void Start()
        {
            // 언어 설정
            Localization.Load();

            // 초기화하고 바뀌지 않는 Controller 설정
            ServiceLocator.Get<UIManager>().TitleController.SetUp();
            ServiceLocator.Get<UIManager>().GameOverController.SetUp();
            ServiceLocator.Get<UIManager>().DemoController.SetUp();

            ChangeState(GameState.Title);
        }

        public void ChangeState(GameState newState)
        {
            OnStateExited();
            CurrentState = newState;
            ServiceLocator.Get<UIManager>().Show(newState);
            OnStateEntered();
        }

        void OnStateExited()
        {
            // 상태 전환 전 처리
            switch (CurrentState)
            {
                case GameState.Title:
                    break;
                case GameState.Main:
                    break;
                case GameState.InGame:
                    break;
                case GameState.GameClear:
                    break;
                case GameState.GameOver:
                    break;
            }
        }

        void OnStateEntered()
        {
            // 상태 전환 후 처리
            switch (CurrentState)
            {
                case GameState.Title:
                    ServiceLocator.Get<AudioManager>().PlayBGM(GameState.Title);
                    break;
                case GameState.Main:
                    ServiceLocator.Get<AudioManager>().PlayBGM(GameState.Main);
                    ServiceLocator.Get<UIManager>().MainController.Refresh();
                    break;
                case GameState.InGame:
                    ServiceLocator.Get<AudioManager>().PlayBGM(GameState.InGame);
                    ServiceLocator.Get<UIManager>().StageController.Refresh();
                    break;
                case GameState.GameClear:
                    break;
                case GameState.GameOver:
                    ServiceLocator.Get<AudioManager>().PlayBGM(GameState.GameOver);
                    break;
                case GameState.Demo:
                    ServiceLocator.Get<AudioManager>().PlayBGM(GameState.Demo);
                    break;
            }
        }

        public void CreateGame()
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
                    Experience = 999,
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

            ServiceLocator.Get<UIManager>().MainController.SetUp();
            ServiceLocator.Get<UIManager>().StageController.SetUp();

            Debug.Log("게임이 생성되었습니다.");
        }

        public void DeleteGame()
        {
            // 게임 종료 로직
            // 예: 플레이어 상태 초기화, 스테이지 리셋 등
            ServiceLocator.Get<UIManager>().MainController.TearDown();
            ServiceLocator.Get<UIManager>().StageController.TearDown();

            Game = null;

            Debug.Log("게임이 종료되었습니다.");
        }

        public void Quit()
        {
            ServiceLocator.Get<UIManager>().TitleController.TearDown();
            ServiceLocator.Get<UIManager>().GameOverController.TearDown();
            ServiceLocator.Get<UIManager>().DemoController.TearDown();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void CreateStage()
        {
            // 스테이지 생성 로직
            // 예: 스테이지 데이터 초기화, 몬스터 생성 등
            ServiceLocator.Get<StageManager>().StartStage();
            Debug.Log("스테이지가 생성되었습니다.");
        }

        public void SaveGame()
        {
            // 게임 저장 로직
            // 예: 플레이어 상태, 스테이지 진행 상황 등을 저장
            Debug.Log("게임이 저장되었습니다.");
        }

        void OnDestroy()
        {
            DOTween.KillAll();
        }
    }
}
