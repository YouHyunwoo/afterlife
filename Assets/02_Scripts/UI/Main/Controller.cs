using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Controller : UI.Controller
    {
        [SerializeField] FocusManager focusManager;

        Screen mainScreen;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            ServiceLocator.Get<AudioManager>().PlayBGM(SceneState.Main);
            RefreshView();
        }

        public override void OnSceneExited(SceneState nextSceneState, UI.Controller nextScreen)
        {
            mainScreen.MenuView.Hide();
            mainScreen.MissionView.Hide();
            mainScreen.UpgradeView.Hide();
            mainScreen.UpgradeInformationView.Hide();
            mainScreen.SettingsView.Hide();
        }

        public override void SetUp()
        {
            mainScreen = Screen as Main.Screen;

            ServiceLocator.Get<LocalizationManager>().OnLanguageChangedEvent += mainScreen.Localize;
            mainScreen.Localize();

            mainScreen.menuButton.onClick.AddListener(OnMenuButtonClicked);
            mainScreen.MenuView.OnContinueButtonClickedEvent += OnContinueButtonClicked;
            mainScreen.MenuView.OnSettingsButtonClickedEvent += OnSettingsButtonClicked;
            mainScreen.MenuView.OnSaveAndQuitButtonClickedEvent += OnSaveAndQuitButtonClicked;
            mainScreen.OrbView.OnButtonClickedEvent += OnOrbClicked;
            mainScreen.MagicCircleView.OnButtonClickedEvent += OnMagicCircleClicked;

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

            mainScreen.menuButton.onClick.RemoveListener(OnMenuButtonClicked);
            mainScreen.MenuView.OnContinueButtonClickedEvent -= OnContinueButtonClicked;
            mainScreen.MenuView.OnSettingsButtonClickedEvent -= OnSettingsButtonClicked;
            mainScreen.MenuView.OnSaveAndQuitButtonClickedEvent -= OnSaveAndQuitButtonClicked;
            mainScreen.OrbView.OnButtonClickedEvent -= OnOrbClicked;
            mainScreen.MagicCircleView.OnButtonClickedEvent -= OnMagicCircleClicked;

            ServiceLocator.Get<LocalizationManager>().OnLanguageChangedEvent -= mainScreen.Localize;

            mainScreen = null;
        }

        void OnMenuButtonClicked() => focusManager.Focus(mainScreen.MenuView);
        void OnContinueButtonClicked() => mainScreen.MenuView.Hide();
        void OnSettingsButtonClicked() => focusManager.Focus(mainScreen.SettingsView);
        void OnSaveAndQuitButtonClicked() => ServiceLocator.Get<GameManager>().QuitGame();
        void OnOrbClicked() => focusManager.Focus(mainScreen.MissionView);
        void OnMagicCircleClicked() => focusManager.Focus(mainScreen.UpgradeView);

        void OnUpgradeItemPurchased(UpgradeNode upgradeNode)
        {
            var gameManager = ServiceLocator.Get<GameManager>();
            var game = gameManager.Game;

            mainScreen.ExperienceView.SetAmount(game.Player.Experience);
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

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (focusManager.IsFocused) { focusManager.Clear(); }
                else { focusManager.Focus(mainScreen.MenuView); }
            }
        }

        public override void RefreshView()
        {
            var game = ServiceLocator.Get<GameManager>().Game;

            mainScreen.ExperienceView.SetAmount(game.Player.Experience);
            mainScreen.LifeView.SetLifes(game.Lives);
            mainScreen.GuideView.SetGuideText("opportunity");
            mainScreen.MissionView.MissionProgressView.SetProgress(game.CurrentStageIndex, game.TotalStageCount);
        }

        public void ResetView()
        {
            var upgradeNodes = mainScreen.UpgradeView.UpgradeTreeView.upgradeNodes;
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.Clear();
            }

            mainScreen.MenuView.Hide();
        }
    }
}
