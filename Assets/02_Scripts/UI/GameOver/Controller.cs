using Afterlife.Core;

namespace Afterlife.UI.GameOver
{
    public class Controller : UI.Controller
    {
        public override void SetUp()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var gameOverScreen = uiManager.GameOverScreen as GameOver.Screen;

            Localization.OnLanguageChangedEvent += gameOverScreen.Localize;
            gameOverScreen.Localize();

            gameOverScreen.OnToTitleButtonClickedEvent += OnToTitleButtonClicked;
        }

        void OnToTitleButtonClicked()
        {
            ServiceLocator.Get<UIManager>().FadeTransition(() =>
            {
                ServiceLocator.Get<GameManager>().ChangeState(GameState.Title);
            });
        }

        public override void TearDown()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var gameOverScreen = uiManager.GameOverScreen as GameOver.Screen;

            Localization.OnLanguageChangedEvent -= gameOverScreen.Localize;

            gameOverScreen.OnToTitleButtonClickedEvent -= OnToTitleButtonClicked;
        }
    }
}