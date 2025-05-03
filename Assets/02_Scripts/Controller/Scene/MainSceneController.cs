using UnityEngine;

namespace Afterlife.Controller
{
    public class MainSceneController : MonoBehaviour
    {
        public void SetUp()
        {
            Controller.Instance.MainView.OnMenuButtonClickedEvent += OnMenuButtonClicked;
            Controller.Instance.MainView.MenuView.OnContinueButtonClickedEvent += OnContinueButtonClicked;
            Controller.Instance.MainView.MenuView.OnSaveAndQuitButtonClickedEvent += OnSaveAndQuitButtonClicked;
            Controller.Instance.MainView.OnStartMissionButtonClickedEvent += OnStartMissionButtonClicked;
        }

        public void TearDown()
        {
            Controller.Instance.MainView.OnMenuButtonClickedEvent -= OnMenuButtonClicked;
            Controller.Instance.MainView.MenuView.OnContinueButtonClickedEvent -= OnContinueButtonClicked;
            Controller.Instance.MainView.MenuView.OnSaveAndQuitButtonClickedEvent -= OnSaveAndQuitButtonClicked;
            Controller.Instance.MainView.OnStartMissionButtonClickedEvent -= OnStartMissionButtonClicked;

            Controller.Instance.MainView.MenuView.Hide();
        }

        void OnMenuButtonClicked()
        {
            Controller.Instance.MainView.MenuView.Show();
        }

        void OnContinueButtonClicked()
        {
            Controller.Instance.MainView.MenuView.Hide();
        }

        void OnSaveAndQuitButtonClicked()
        {
            SaveGame();
            TearDown();
            TransitToTitleScene();
        }

        void SaveGame() {}

        void TransitToTitleScene()
        {
            Controller.Instance.TitleView.Show();
            Controller.Instance.MainView.Hide();
        }

        void OnStartMissionButtonClicked()
        {
            StartMission();
            TransitToStageScene();
        }

        void StartMission()
        {
            Controller.Instance.StageSceneController.SetUp();
        }

        void TransitToStageScene()
        {
            Controller.Instance.StageView.Show();
            Controller.Instance.MainView.Hide();
        }

        public void RefreshView()
        {
            var game = Controller.Instance.Game;

            Controller.Instance.MainView.SetLifes(game.Lifes);
            Controller.Instance.MainView.SetStageProgress(game.CurrentStageIndex, game.TotalStageCount);
        }
    }
}