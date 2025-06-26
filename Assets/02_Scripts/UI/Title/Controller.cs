using System;
using Afterlife.Core;

namespace Afterlife.UI.Title
{
    public class Controller : UI.Controller
    {
        Screen titleScreen;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            ServiceLocator.Get<AudioManager>().PlayBGM(SceneState.Title);
        }

        public override void SetUp()
        {
            titleScreen = Screen as Title.Screen;

            Localization.OnLanguageChangedEvent += titleScreen.Localize;
            titleScreen.Localize();

            titleScreen.OnNewGameButtonClickedEvent += OnNewGameButtonClicked;
            titleScreen.OnSettingsButtonClickedEvent += OnSettingsButtonClicked;
            titleScreen.OnExitButtonClickedEvent += OnExitButtonClicked;

            titleScreen.SettingsView.OnLanguageButtonClickedEvent += OnLanguageButtonClicked;
        }

        public override void TearDown()
        {
            titleScreen.OnNewGameButtonClickedEvent -= OnNewGameButtonClicked;
            titleScreen.OnSettingsButtonClickedEvent -= OnSettingsButtonClicked;
            titleScreen.OnExitButtonClickedEvent -= OnExitButtonClicked;

            Localization.OnLanguageChangedEvent -= titleScreen.Localize;

            titleScreen = null;
        }

        void OnNewGameButtonClicked() => ServiceLocator.Get<GameManager>().StartGame();
        void OnSettingsButtonClicked() => titleScreen.SettingsView.Show();
        void OnExitButtonClicked() => ServiceLocator.Get<ApplicationManager>().Quit();

        void OnLanguageButtonClicked()
        {
            var rotatedLanguage = (int)(Localization.CurrentLanguage + 1) % Enum.GetValues(typeof(Language)).Length;
            Localization.SetLanguage((Language)rotatedLanguage);
        }
    }
}