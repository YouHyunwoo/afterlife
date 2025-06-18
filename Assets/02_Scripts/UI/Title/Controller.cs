using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI.Title
{
    public class Controller : UI.Controller
    {
        public override void SetUp()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var titleView = uiManager.TitleScreen as Title.Screen;

            titleView.OnNewGameButtonClickedEvent += OnNewGameButtonClicked;
            titleView.OnExitButtonClickedEvent += OnExitButtonClicked;
        }

        public override void TearDown()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var titleView = uiManager.TitleScreen as Title.Screen;

            titleView.OnNewGameButtonClickedEvent -= OnNewGameButtonClicked;
            titleView.OnExitButtonClickedEvent -= OnExitButtonClicked;
        }

        void OnNewGameButtonClicked()
        {
            ServiceLocator.Get<GameManager>().CreateGame();
            ServiceLocator.Get<GameManager>().ChangeState(GameState.Main);
        }

        void OnExitButtonClicked()
        {
            ServiceLocator.Get<GameManager>().DeleteGame();
            ServiceLocator.Get<GameManager>().Quit();
        }
    }
}