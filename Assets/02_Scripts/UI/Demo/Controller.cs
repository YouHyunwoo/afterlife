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

            Localization.OnLanguageChangedEvent += demoScreen.Localize;
            demoScreen.Localize();

            demoScreen.OnToTitleButtonClickedEvent += OnToTitleButtonClicked;
        }

        public override void TearDown()
        {
            Localization.OnLanguageChangedEvent -= demoScreen.Localize;

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