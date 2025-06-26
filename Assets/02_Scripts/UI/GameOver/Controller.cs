using Afterlife.Core;

namespace Afterlife.UI.GameOver
{
    public class Controller : UI.Controller
    {
        Screen gameOverScreen;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            ServiceLocator.Get<AudioManager>().PlayBGM(SceneState.GameOver);
        }

        public override void SetUp()
        {
            gameOverScreen = Screen as GameOver.Screen;

            Localization.OnLanguageChangedEvent += gameOverScreen.Localize;
            gameOverScreen.Localize();

            gameOverScreen.OnToTitleButtonClickedEvent += OnToTitleButtonClicked;
        }

        public override void TearDown()
        {
            Localization.OnLanguageChangedEvent -= gameOverScreen.Localize;

            gameOverScreen.OnToTitleButtonClickedEvent -= OnToTitleButtonClicked;

            gameOverScreen = null;
        }

        void OnToTitleButtonClicked()
        {
            ServiceLocator.Get<UIManager>().FadeTransition(() =>
            {
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Title);
            });
        }
    }
}