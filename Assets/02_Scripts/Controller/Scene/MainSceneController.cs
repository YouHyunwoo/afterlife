using UnityEngine;

namespace Afterlife.Controller
{
    public class MainSceneController : MonoBehaviour
    {
        [Header("Controller")]
        public StageSceneController StageSceneController;

        [Header("View")]
        public View.Title TitleView;
        public View.Main MainView;
        public View.Stage StageView;

        public Model.Game Game;

        public void Initialize(Model.Game game)
        {
            Game = game;
            MainView.SetLifes(game.Lifes);
            MainView.SetStageProgress(game.CurrentStageIndex, game.TotalStageCount);
        }

        public void StartMission()
        {
            MainView.Hide();
            StageView.Show();
            StageSceneController.StartStage(Game);
        }

        public void QuitGame()
        {
            TitleView.Show();
            MainView.Hide();
        }
    }
}