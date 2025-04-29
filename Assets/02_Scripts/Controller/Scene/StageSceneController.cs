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

        public Model.Game Game;

        public void StartStage(Model.Game game)
        {
            Game = game;
            StageController.StartStage(game.Data.stageData, Game.Player);
        }

        public void FinishStage(bool isClear)
        {
            StageView.Hide();
            MainView.Show();

            if (isClear)
            {
                Game.CurrentStageIndex++;
                MainView.SetStageProgress(Game.CurrentStageIndex, Game.TotalStageCount);
            }
            else
            {
                Game.Lifes--;
                if (Game.Lifes <= 0)
                {
                    Game.Lifes = 0;
                    // TODO: Game Over 로직 추가
                }
                MainView.SetLifes(Game.Lifes);
            }

            Game = null;
        }
    }
}