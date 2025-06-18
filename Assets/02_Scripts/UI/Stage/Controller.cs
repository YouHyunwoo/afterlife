using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class Controller : UI.Controller
    {
        public override void SetUp()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as Stage.Screen;

            stageScreen.OnMenuButtonClickedEvent += OnMenuButtonClicked;
        }

        void OnMenuButtonClicked()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as Stage.Screen;
            stageScreen.MenuView.Show();
        }

        public override void Refresh()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var stageScreen = uiManager.InGameScreen as Stage.Screen;
            var gameManager = ServiceLocator.Get<GameManager>();
            var game = gameManager.Game;

            stageScreen.SetExperience(game.Player.Experience);
        }
    }
}