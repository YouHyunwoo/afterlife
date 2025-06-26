using Afterlife.Core;

namespace Afterlife.UI.Stage
{
    public class Controller : UI.Controller
    {
        Screen stageScreen;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            ServiceLocator.Get<AudioManager>().PlayBGM(SceneState.InGame);
            ServiceLocator.Get<UIManager>().StageController.RefreshView();
        }

        public override void OnSceneExited(SceneState nextSceneState, UI.Controller nextScreen)
        {
            stageScreen.MenuView.Hide();
            stageScreen.SkillInformationView.Hide();
            stageScreen.CraftView.Hide();
        }

        public override void SetUp()
        {
            stageScreen = Screen as Stage.Screen;

            Localization.OnLanguageChangedEvent += stageScreen.Localize;
            stageScreen.Localize();

            stageScreen.OnMenuButtonClickedEvent += OnMenuButtonClicked;
            stageScreen.MenuView.OnContinueButtonClickedEvent += OnContinueButtonClicked;
            stageScreen.MenuView.OnGiveUpButtonClickedEvent += OnGiveUpButtonClicked;
        }

        public override void TearDown()
        {
            stageScreen.OnMenuButtonClickedEvent -= OnMenuButtonClicked;
            stageScreen.MenuView.OnContinueButtonClickedEvent -= OnContinueButtonClicked;
            stageScreen.MenuView.OnGiveUpButtonClickedEvent -= OnGiveUpButtonClicked;

            Localization.OnLanguageChangedEvent -= stageScreen.Localize;

            stageScreen = null;
        }

        void OnMenuButtonClicked() => stageScreen.MenuView.Show();
        void OnContinueButtonClicked() => stageScreen.MenuView.Hide();
        void OnGiveUpButtonClicked() => ServiceLocator.Get<StageManager>().FailStage();

        public override void RefreshView()
        {
            var gameManager = ServiceLocator.Get<GameManager>();
            var game = gameManager.Game;

            stageScreen.ExperienceView.SetAmount(game.Player.Experience);
            stageScreen.MenuView.MissionProgressView.SetProgress(game.CurrentStageIndex, game.TotalStageCount);
        }
    }
}