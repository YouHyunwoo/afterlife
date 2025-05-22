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

            SetUpViews();
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

        void SaveGame() { }

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

        void SetUpViews()
        {
            // 상점 뷰 셋업
            Controller.Instance.MainView.PowerView.HeroStoreContentView.SetStoreContent(Controller.Instance.StoreDataArray[0]);
            Controller.Instance.MainView.PowerView.AbilityStoreContentView.SetStoreContent(Controller.Instance.StoreDataArray[1]);

            Controller.Instance.MainView.PowerView.HeroStoreContentView.OnStoreItemClickedEvent += OnStoreItemClicked;
            Controller.Instance.MainView.PowerView.AbilityStoreContentView.OnStoreItemClickedEvent += OnStoreItemClicked;
        }

        void OnStoreItemClicked(string itemId)
        {
            Debug.Log($"Store Item Clicked: {itemId}");
        }
    }
}