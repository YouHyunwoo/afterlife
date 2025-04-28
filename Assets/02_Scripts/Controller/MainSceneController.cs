using UnityEngine;

namespace Afterlife.Controller
{
    public class MainSceneController : MonoBehaviour
    {
        [Header("Controller")]
        public StageController StageController;

        [Header("View")]
        public View.Title TitleView;
        public View.Main MainView;
        public View.Stage StageView;
        public View.TerrainTileIndicator TerrainTileIndicator;

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
            StageController.StartStage(Game.Player);
            TerrainTileIndicator.gameObject.SetActive(true);
        }

        public void QuitGame()
        {
            TitleView.Show();
            MainView.Hide();
        }
    }
}