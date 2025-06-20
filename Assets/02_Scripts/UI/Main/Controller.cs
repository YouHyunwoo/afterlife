using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Controller : UI.Controller
    {
        public override void SetUp()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var mainScreen = uiManager.MainScreen as Main.Screen;

            Localization.OnLanguageChangedEvent += mainScreen.Localize;
            mainScreen.Localize();

            mainScreen.GetComponent<Tab>().SetView(0);

            mainScreen.OnMenuButtonClickedEvent += OnMenuButtonClicked;
            mainScreen.MenuView.OnContinueButtonClickedEvent += OnContinueButtonClicked;
            mainScreen.MenuView.OnSaveAndQuitButtonClickedEvent += OnSaveAndQuitButtonClicked;
            mainScreen.OnStartMissionButtonClickedEvent += OnStartMissionButtonClicked;

            var upgradeNodes = mainScreen.UpgradeView.UpgradeTreeView.upgradeNodes;
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.OnPurchased += OnUpgradeItemPurchased;
                upgradeNode.OnInformationShowed += OnUpgradeNodeInformationShowed;
                upgradeNode.OnInformationHidden += OnUpgradeNodeInformationHidden;
                upgradeNode.Clear();
            }
        }

        void OnUpgradeItemPurchased(UpgradeNode upgradeNode)
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var mainScreen = uiManager.MainScreen as Main.Screen;
            var gameManager = ServiceLocator.Get<GameManager>();
            var game = gameManager.Game;

            mainScreen.UpgradeView.ExperienceView.SetAmount(game.Player.Experience);
        }

        void OnUpgradeNodeInformationShowed(UpgradeNode upgradeNode)
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var mainScreen = uiManager.MainScreen as Main.Screen;

            var nodeRectTransform = upgradeNode.GetComponent<RectTransform>();
            mainScreen.UpgradeInformationView.GetComponent<RectTransform>().position = nodeRectTransform.position + new Vector3(20, 20, 0);
            mainScreen.UpgradeInformationView.Show(upgradeNode.Id, upgradeNode.Cost, upgradeNode.State);
        }

        void OnUpgradeNodeInformationHidden(UpgradeNode upgradeNode)
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var mainScreen = uiManager.MainScreen as Main.Screen;

            mainScreen.UpgradeInformationView.Hide();
        }

        public override void TearDown()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var mainScreen = uiManager.MainScreen as Main.Screen;

            Localization.OnLanguageChangedEvent -= mainScreen.Localize;

            mainScreen.OnMenuButtonClickedEvent -= OnMenuButtonClicked;
            mainScreen.MenuView.OnContinueButtonClickedEvent -= OnContinueButtonClicked;
            mainScreen.MenuView.OnSaveAndQuitButtonClickedEvent -= OnSaveAndQuitButtonClicked;
            mainScreen.OnStartMissionButtonClickedEvent -= OnStartMissionButtonClicked;

            var upgradeNodes = mainScreen.GetComponentsInChildren<UpgradeNode>(true);
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.OnPurchased -= OnUpgradeItemPurchased;
                upgradeNode.OnInformationShowed -= OnUpgradeNodeInformationShowed;
                upgradeNode.OnInformationHidden -= OnUpgradeNodeInformationHidden;
            }
        }

        void OnMenuButtonClicked()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var mainScreen = uiManager.MainScreen as Main.Screen;

            mainScreen.MenuView.Show();
        }

        void OnContinueButtonClicked()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var mainScreen = uiManager.MainScreen as Main.Screen;

            mainScreen.MenuView.Hide();
        }

        void OnSaveAndQuitButtonClicked()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var mainScreen = uiManager.MainScreen as Main.Screen;

            mainScreen.MenuView.Hide();

            ServiceLocator.Get<UIManager>().FadeTransition(() =>
            {
                ServiceLocator.Get<GameManager>().SaveGame();
                ServiceLocator.Get<GameManager>().DeleteGame();
                ServiceLocator.Get<GameManager>().ChangeState(GameState.Title);
            });
        }

        void OnStartMissionButtonClicked()
        {
            ServiceLocator.Get<UIManager>().FadeTransition(() =>
            {
                ServiceLocator.Get<GameManager>().CreateStage();
                ServiceLocator.Get<GameManager>().ChangeState(GameState.InGame);
            });
        }

        public override void Refresh()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            var mainScreen = uiManager.MainScreen as Main.Screen;
            var game = ServiceLocator.Get<GameManager>().Game;

            mainScreen.MissionView.LifeView.SetLifes(game.Lifes);
            mainScreen.MissionView.MissionProgressView.SetProgress(game.CurrentStageIndex, game.TotalStageCount);
            mainScreen.UpgradeView.ExperienceView.SetAmount(game.Player.Experience);
        }
    }
}
