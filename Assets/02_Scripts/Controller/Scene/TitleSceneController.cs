using UnityEngine;

namespace Afterlife.Controller
{
    public class TitleSceneController : MonoBehaviour
    {
        [Header("Data")]
        public Data.Game GameData;

        [Header("Controller")]
        public MainSceneController MainSceneController;

        [Header("View")]
        public View.Title TitleView;
        public View.Main MainView;

        public Model.Game Game;

        public void StartGame()
        {
            TitleView.Hide();
            MainView.Show();

            Game = new Model.Game
            {
                Data = GameData,
                Lifes = GameData.Lifes,
                Player = new Model.Player
                {
                    Level = 1,
                    Experience = 0,
                    MaxExperience = 10,
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
                        Location = new Vector2Int(),
                        Intensity = 3f,
                        Range = 5f,
                        IsActive = false,
                    },
                },
                CurrentStageIndex = 0,
                TotalStageCount = GameData.TotalStageCount,
            };

            MainSceneController.Initialize(Game);
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}