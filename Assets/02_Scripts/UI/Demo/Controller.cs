using Afterlife.Core;

namespace Afterlife.UI.Demo
{
    public class Controller : UI.Controller
    {
        Screen demoScreen;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            ServiceLocator.Get<AudioManager>().PlayBGM(SceneState.Demo);
        }

        public override void SetUp()
        {
            demoScreen = Screen as Demo.Screen;

            ServiceLocator.Get<LocalizationManager>().OnLanguageChangedEvent += demoScreen.Localize;
            demoScreen.Localize();

            demoScreen.OnToTitleButtonClickedEvent += OnToTitleButtonClicked;
        }

        public override void TearDown()
        {
            ServiceLocator.Get<LocalizationManager>().OnLanguageChangedEvent -= demoScreen.Localize;

            demoScreen.OnToTitleButtonClickedEvent -= OnToTitleButtonClicked;

            demoScreen = null;
        }

        void OnToTitleButtonClicked()
        {
            ServiceLocator.Get<UIManager>().Fade(() =>
            {
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Title);
            });
        }
    }
}