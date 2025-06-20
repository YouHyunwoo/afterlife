using Afterlife.Core;

namespace Afterlife.UI.Stage
{
    public class Controller : UI.Controller
    {
        public override void SetUp()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as Stage.Screen;

            Localization.OnLanguageChangedEvent += stageScreen.Localize;
            stageScreen.Localize();

            stageScreen.OnMenuButtonClickedEvent += OnMenuButtonClicked;
            stageScreen.MenuView.OnContinueButtonClickedEvent += OnContinueButtonClicked;
            stageScreen.MenuView.OnGiveUpButtonClickedEvent += OnGiveUpButtonClicked;
        }

        void OnMenuButtonClicked()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as Stage.Screen;
            stageScreen.MenuView.Show();
        }

        void OnContinueButtonClicked()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as Stage.Screen;

            stageScreen.MenuView.Hide();
        }

        void OnGiveUpButtonClicked()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as Stage.Screen;

            stageScreen.MenuView.Hide();

            ServiceLocator.Get<StageManager>().FailStage();
        }

        public override void TearDown()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as Stage.Screen;

            Localization.OnLanguageChangedEvent -= stageScreen.Localize;

            stageScreen.OnMenuButtonClickedEvent -= OnMenuButtonClicked;
            stageScreen.MenuView.OnContinueButtonClickedEvent -= OnContinueButtonClicked;
            stageScreen.MenuView.OnGiveUpButtonClickedEvent -= OnGiveUpButtonClicked;
        }

        public override void Refresh()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as Stage.Screen;
            var gameManager = ServiceLocator.Get<GameManager>();
            var game = gameManager.Game;

            stageScreen.ExperienceView.SetAmount(game.Player.Experience);
            stageScreen.MenuView.MissionProgressView.SetProgress(game.CurrentStageIndex, game.TotalStageCount);
        }
    }
}