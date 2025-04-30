using UnityEngine;

namespace Afterlife.Controller
{
    public class StageSceneController : MonoBehaviour
    {
        [Header("Controller")]
        public StageController StageController;

        [Header("View")]
        public View.Main MainView;
        public View.Stage StageView;
        public View.Demo DemoView;
        public View.GameOver GameOverView;

        public Model.Game Game;

        public void StartStage(Model.Game game)
        {
            Game = game;
            StageController.StartStage(game.Data.stageData, Game.Player);
            StageView.SetExperienceRatio(game.Player.Experience / game.Player.MaxExperience);
        }

        public void FinishStage(bool isClear)
        {
            StageView.Hide();

            if (isClear)
            {
                Game.CurrentStageIndex++;
                if (Game.CurrentStageIndex >= Game.TotalStageCount)
                {
                    DemoView.Show();
                }
                else
                {
                    MainView.SetStageProgress(Game.CurrentStageIndex, Game.TotalStageCount);
                    MainView.Show();
                }
            }
            else
            {
                Game.Lifes--;
                if (Game.Lifes <= 0)
                {
                    Game.Lifes = 0;
                    GameOverView.Show();
                }
                else
                {
                    MainView.SetLifes(Game.Lifes);
                    MainView.Show();
                }
            }

            Game = null;
        }
    }
}