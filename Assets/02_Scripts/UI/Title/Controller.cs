using System;
using Afterlife.Core;

namespace Afterlife.UI.Title
{
    public class Controller : UI.Controller
    {
        public override void SetUp()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var titleView = uiManager.TitleScreen as Title.Screen;

            Localization.OnLanguageChangedEvent += titleView.Localize;
            titleView.Localize();

            titleView.OnNewGameButtonClickedEvent += OnNewGameButtonClicked;
            titleView.OnSettingsButtonClickedEvent += OnSettingsButtonClicked;
            titleView.OnExitButtonClickedEvent += OnExitButtonClicked;

            titleView.SettingsView.OnLanguageButtonClickedEvent += OnLanguageButtonClicked;
        }

        public override void TearDown()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var titleView = uiManager.TitleScreen as Title.Screen;

            Localization.OnLanguageChangedEvent -= titleView.Localize;

            titleView.OnNewGameButtonClickedEvent -= OnNewGameButtonClicked;
            titleView.OnSettingsButtonClickedEvent -= OnSettingsButtonClicked;
            titleView.OnExitButtonClickedEvent -= OnExitButtonClicked;
        }

        void OnNewGameButtonClicked()
        {
            ServiceLocator.Get<UIManager>().FadeOut(() =>
            {
                ServiceLocator.Get<GameManager>().CreateGame();
                ServiceLocator.Get<GameManager>().ChangeState(GameState.Main);
                ServiceLocator.Get<UIManager>().FadeIn(() => { });
            });
        }

        void OnSettingsButtonClicked()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var titleView = uiManager.TitleScreen as Title.Screen;

            titleView.SettingsView.Show();
        }

        void OnExitButtonClicked()
        {
            ServiceLocator.Get<GameManager>().Quit();
        }

        void OnLanguageButtonClicked()
        {
            var rotatedLanguage = (int)(Localization.CurrentLanguage + 1) % Enum.GetValues(typeof(Language)).Length;
            Localization.SetLanguage((Language)rotatedLanguage);
        }
    }
}