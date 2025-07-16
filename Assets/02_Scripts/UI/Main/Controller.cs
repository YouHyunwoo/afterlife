using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Controller : UI.Controller
    {
        Screen mainScreen;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            ServiceLocator.Get<AudioManager>().PlayBGM(SceneState.Main);
            RefreshView();
        }

        public override void OnSceneExited(SceneState nextSceneState, UI.Controller nextScreen)
        {
            mainScreen.MenuView.Hide();
        }

        public override void SetUp()
        {
            mainScreen = Screen as Main.Screen;

            Localization.OnLanguageChangedEvent += mainScreen.Localize;
            mainScreen.Localize();

            mainScreen.OnMenuButtonClickedEvent += OnMenuButtonClicked;
            mainScreen.MenuView.OnContinueButtonClickedEvent += OnContinueButtonClicked;
            mainScreen.MenuView.OnSettingsButtonClickedEvent += OnSettingsButtonClicked;
            mainScreen.MenuView.OnSaveAndQuitButtonClickedEvent += OnSaveAndQuitButtonClicked;
            mainScreen.OnStartMissionButtonClickedEvent += OnStartMissionButtonClicked;

            var upgradeNodes = mainScreen.UpgradeView.UpgradeTreeView.upgradeNodes;
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.OnPurchased += OnUpgradeItemPurchased;
                upgradeNode.OnInformationShowed += OnUpgradeNodeInformationShowed;
                upgradeNode.OnInformationHidden += OnUpgradeNodeInformationHidden;
            }
        }

        public override void TearDown()
        {
            var upgradeNodes = mainScreen.UpgradeView.UpgradeTreeView.upgradeNodes;
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.OnPurchased -= OnUpgradeItemPurchased;
                upgradeNode.OnInformationShowed -= OnUpgradeNodeInformationShowed;
                upgradeNode.OnInformationHidden -= OnUpgradeNodeInformationHidden;
            }

            mainScreen.OnMenuButtonClickedEvent -= OnMenuButtonClicked;
            mainScreen.MenuView.OnContinueButtonClickedEvent -= OnContinueButtonClicked;
            mainScreen.MenuView.OnSettingsButtonClickedEvent -= OnSettingsButtonClicked;
            mainScreen.MenuView.OnSaveAndQuitButtonClickedEvent -= OnSaveAndQuitButtonClicked;
            mainScreen.OnStartMissionButtonClickedEvent -= OnStartMissionButtonClicked;

            Localization.OnLanguageChangedEvent -= mainScreen.Localize;

            mainScreen = null;
        }

        void OnMenuButtonClicked() => mainScreen.MenuView.Show();
        void OnContinueButtonClicked() => mainScreen.MenuView.Hide();
        void OnSettingsButtonClicked() => mainScreen.SettingsView.Show();
        void OnSaveAndQuitButtonClicked() => ServiceLocator.Get<GameManager>().QuitGame();
        void OnStartMissionButtonClicked() => ServiceLocator.Get<GameManager>().StartStage();

        void OnUpgradeItemPurchased(UpgradeNode upgradeNode)
        {
            var gameManager = ServiceLocator.Get<GameManager>();
            var game = gameManager.Game;

            mainScreen.UpgradeView.ExperienceView.SetAmount(game.Player.Experience);
        }

        void OnUpgradeNodeInformationShowed(UpgradeNode upgradeNode)
        {
            var nodeRectTransform = upgradeNode.GetComponent<RectTransform>();
            mainScreen.UpgradeInformationView.GetComponent<RectTransform>().position = nodeRectTransform.position + new Vector3(20, 20, 0);
            mainScreen.UpgradeInformationView.Show(upgradeNode.UpgradeData.Id, upgradeNode.UpgradeData.Cost, upgradeNode.State);
        }

        void OnUpgradeNodeInformationHidden(UpgradeNode upgradeNode)
        {
            mainScreen.UpgradeInformationView.Hide();
        }

        public override void RefreshView()
        {
            mainScreen.GetComponent<Tab>().SetView(0);

            var game = ServiceLocator.Get<GameManager>().Game;

            mainScreen.MissionView.LifeView.SetLifes(game.Lives);
            mainScreen.MissionView.MissionProgressView.SetProgress(game.CurrentStageIndex, game.TotalStageCount);
            mainScreen.UpgradeView.ExperienceView.SetAmount(game.Player.Experience);
        }

        public void ResetView()
        {
            mainScreen.GetComponent<Tab>().SetView(0);

            var upgradeNodes = mainScreen.UpgradeView.UpgradeTreeView.upgradeNodes;
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.Clear();
            }

            mainScreen.MenuView.Hide();
        }
    }
}
