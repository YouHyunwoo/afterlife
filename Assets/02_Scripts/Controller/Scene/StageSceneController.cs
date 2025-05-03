using UnityEngine;

namespace Afterlife.Controller
{
    public class StageSceneController : MonoBehaviour
    {
        public StageController StageController;

        public void SetUp()
        {
            StageController.SetUp();
            StageController.OnStageClearedEvent += OnStageCleared;
            StageController.OnStageFailedEvent += OnStageFailed;
            StageController.OnGameClearedEvent += OnGameCleared;
            StageController.OnGameOverEvent += OnGameOver;

            Controller.Instance.StageView.SetExperience(Controller.Instance.Game.Player.Experience);
        }

        void OnStageCleared()
        {
            TearDown();
            var game = Controller.Instance.Game;
            Controller.Instance.MainView.SetStageProgress(game.CurrentStageIndex, game.TotalStageCount);
            Controller.Instance.MainView.PowerView.ExperienceView.SetExperience(game.Player.Experience);
            TransitToMainScene();
        }

        void TransitToMainScene()
        {
            Controller.Instance.MainView.Show();
            Controller.Instance.StageView.Hide();
        }

        void OnStageFailed()
        {
            TearDown();
            var game = Controller.Instance.Game;
            Controller.Instance.MainView.SetLifes(game.Lifes);
            Controller.Instance.MainView.PowerView.ExperienceView.SetExperience(game.Player.Experience);
            TransitToMainScene();
        }

        void OnGameCleared()
        {
            TearDown();
            TransitToDemoScene();
        }

        void TransitToDemoScene()
        {
            Controller.Instance.DemoView.Show();
            Controller.Instance.StageView.Hide();
        }

        void OnGameOver()
        {
            TearDown();
            TransitToGameOverScene();
        }

        void TransitToGameOverScene()
        {
            Controller.Instance.GameOverView.Show();
            Controller.Instance.StageView.Hide();
        }

        public void TearDown()
        {
            StageController.OnStageClearedEvent -= OnStageCleared;
            StageController.OnStageFailedEvent -= OnStageFailed;
            StageController.OnGameClearedEvent -= OnGameCleared;
            StageController.OnGameOverEvent -= OnGameOver;
            StageController.TearDown();
        }
    }
}