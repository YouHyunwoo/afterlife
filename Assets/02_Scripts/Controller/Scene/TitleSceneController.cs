using UnityEngine;

namespace Afterlife.Controller
{
    public class TitleSceneController : MonoBehaviour
    {
        void Awake()
        {
            Controller.Instance.TitleView.OnNewGameButtonClickedEvent += OnNewGameButtonClicked;
            Controller.Instance.TitleView.OnExitButtonClickedEvent += OnExitButtonClicked;
        }

        void OnNewGameButtonClicked()
        {
            var gameData = Controller.Instance.GameData; // TODO: 게임 데이터 가져오기
            var game = CreateGame(gameData);
            var isGameVerified = VerifyGame(game);
            if (!isGameVerified) { return; }
            StartGame(game);
            SetUpMainScene();
            TransitToMainScene();
        }

        Model.Game CreateGame(Data.Game gameData)
        {
            return new Model.Game
            {
                Data = gameData,
                Lifes = gameData.Lifes,
                Player = new Model.Player
                {
                    Experience = 0,
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
                    Skills = new()
                    {
                        new Model.RichResources(Controller.Instance.SkillDataDictionary["rich-resources"]),
                    }
                },
                CurrentStageIndex = 0,
                TotalStageCount = gameData.StageDataArray.Length,
            };
        }

        bool VerifyGame(Model.Game game)
        {
            if (game == null)
            {
                Debug.LogError("Game object is null.");
                return false;
            }

            if (game.Data == null)
            {
                Debug.LogError("Game data is null.");
                return false;
            }

            if (game.Player == null)
            {
                Debug.LogError("Player object is null.");
                return false;
            }

            if (game.CurrentStageIndex < 0 || game.CurrentStageIndex >= game.TotalStageCount)
            {
                Debug.LogError("Current stage index is out of bounds.");
                return false;
            }

            if (game.Data.StageDataArray == null || game.Data.StageDataArray.Length == 0)
            {
                Debug.LogError("Stage data array is null or empty.");
                return false;
            }

            return true;
        }

        void StartGame(Model.Game game)
        {
            Controller.Instance.Game = game;
        }

        void SetUpMainScene()
        {
            Controller.Instance.MainSceneController.SetUp();
            Controller.Instance.MainSceneController.RefreshView();
        }

        void TransitToMainScene()
        {
            Controller.Instance.MainView.Show();
            Controller.Instance.TitleView.Hide();
        }

        void OnExitButtonClicked()
        {
            ReleaseGame();
            ExitGame();
        }

        void ReleaseGame()
        {
            Controller.Instance.Game = null;
        }

        void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}