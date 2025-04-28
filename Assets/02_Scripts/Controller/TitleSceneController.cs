using UnityEngine;

namespace Afterlife.Controller
{
    public class TitleSceneController : MonoBehaviour
    {
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
                Lifes = 3,
                Player = new Model.Player
                {
                    AttackPower = 1f,
                    AttackSpeed = 1f,
                    AttackRange = 1f,
                    AttackCount = 1f,
                    CriticalRate = 0.0f,
                    CriticalDamageMultiplier = 1.2f
                },
                CurrentStageIndex = 0,
                TotalStageCount = 4,
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