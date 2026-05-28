using Afterlife.Core;
using Afterlife.Domain.Progression.Upgrade.UI;
using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Controller : UI.Controller
    {
        [SerializeField] FocusManager focusManager;
        [SerializeField] Tutorial tutorialView;

        Screen mainScreen;

        bool isTutorialActive;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            ServiceLocator.Get<AudioManager>().PlayBGM(SceneState.Main);
            RefreshView();

            isTutorialActive = ServiceLocator.Get<GameManager>().Game.IsMainTutorialActive;
            if (isTutorialActive)
            {
                enabled = false;
                tutorialView.OnFinishedEvent += OnTutorialFinished;
                tutorialView.Show();
                tutorialView.StartTutorial();
            }
        }

        void OnTutorialFinished()
        {
            ServiceLocator.Get<GameManager>().Game.IsMainTutorialActive = false;
            tutorialView.OnFinishedEvent -= OnTutorialFinished;
            enabled = true;
        }

        public override void OnSceneExited(SceneState nextSceneState, UI.Controller nextScreen)
        {
            mainScreen.MenuView.Hide();
            mainScreen.MissionView.Hide();
            mainScreen.UpgradePanel.Hide();
            if (mainScreen.ArtifactView != null) { mainScreen.ArtifactView.Hide(); }
            if (mainScreen.RecipeView != null) { mainScreen.RecipeView.Hide(); }
            mainScreen.UpgradeTooltipView.Hide();
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
            mainScreen.MissionView.OnButtonClickedEvent += OnMissionButtonClicked;
            if (mainScreen.ArtifactView != null)
            {
                mainScreen.ArtifactView.OnArtifactListItemClickedEvent += OnArtifactListItemClicked;
            }
            if (mainScreen.RecipeView != null)
            {
                mainScreen.RecipeView.OnRecipeUnlockedEvent += OnRecipeUnlocked;
                mainScreen.RecipeView.OnRecipeInformationShowedEvent += OnRecipeInformationShowed;
                mainScreen.RecipeView.OnRecipeInformationHiddenEvent += OnRecipeInformationHidden;
            }

            var upgradeNodes = mainScreen.UpgradePanel.UpgradeTreeView.upgradeNodes;
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.OnPurchased += OnUpgradeItemPurchased;
                upgradeNode.OnInformationShowed += OnUpgradeNodeInformationShowed;
                upgradeNode.OnInformationHidden += OnUpgradeNodeInformationHidden;
            }
        }

        public override void TearDown()
        {
            var upgradeNodes = mainScreen.UpgradePanel.UpgradeTreeView.upgradeNodes;
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
            mainScreen.MissionView.OnButtonClickedEvent -= OnMissionButtonClicked;
            if (mainScreen.ArtifactView != null)
            {
                mainScreen.ArtifactView.OnArtifactListItemClickedEvent -= OnArtifactListItemClicked;
            }
            if (mainScreen.RecipeView != null)
            {
                mainScreen.RecipeView.OnRecipeUnlockedEvent -= OnRecipeUnlocked;
                mainScreen.RecipeView.OnRecipeInformationShowedEvent -= OnRecipeInformationShowed;
                mainScreen.RecipeView.OnRecipeInformationHiddenEvent -= OnRecipeInformationHidden;
            }

            ServiceLocator.Get<LocalizationManager>().OnLanguageChangedEvent -= mainScreen.Localize;

            mainScreen = null;
        }

        void OnMenuButtonClicked() => focusManager.Focus(mainScreen.MenuView);
        void OnContinueButtonClicked() => mainScreen.MenuView.Hide();
        void OnSettingsButtonClicked() => focusManager.Focus(mainScreen.SettingsView);
        void OnSaveAndQuitButtonClicked() => ServiceLocator.Get<GameManager>().QuitGame();
        void OnOrbClicked() => focusManager.Focus(mainScreen.MissionView);
        void OnMagicCircleClicked() => focusManager.Focus(mainScreen.UpgradePanel);
        void OnMissionButtonClicked() => ServiceLocator.Get<GameManager>().StartStage();

        void OnUpgradeItemPurchased(UpgradeNodeView upgradeNode)
        {
            var gameManager = ServiceLocator.Get<GameManager>();
            var game = gameManager.Game;

            mainScreen.ExperienceView.SetAmount(game.Player.Experience);
            RefreshArtifactView();
            RefreshRecipeView();
        }

        void OnArtifactListItemClicked(ArtifactListItem item)
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            if (game.Artifact.TrySelectArtifact(item.ArtifactId))
            {
                RefreshView();
            }
        }

        void OnRecipeUnlocked(RecipeNode node)
        {
            RefreshView();
        }

        void OnRecipeInformationShowed(RecipeNode node)
        {
            if (node.RecipeData == null) { return; }

            var nodeRectTransform = node.GetComponent<RectTransform>();
            mainScreen.UpgradeTooltipView.GetComponent<RectTransform>().position =
                nodeRectTransform.position + new Vector3(20, 20, 0);
            mainScreen.UpgradeTooltipView.Show(node.RecipeData.ItemId, node.RecipeData.Cost, UpgradeNodeView.UpgradeState.Unlocked);
        }

        void OnRecipeInformationHidden(RecipeNode node)
        {
            mainScreen.UpgradeTooltipView.Hide();
        }

        void OnUpgradeNodeInformationShowed(UpgradeNodeView upgradeNode)
        {
            var nodeRectTransform = upgradeNode.GetComponent<RectTransform>();
            mainScreen.UpgradeTooltipView.GetComponent<RectTransform>().position = nodeRectTransform.position + new Vector3(20, 20, 0);
            mainScreen.UpgradeTooltipView.Show(upgradeNode.UpgradeData.Id, upgradeNode.UpgradeData.Cost, upgradeNode.State);
        }

        void OnUpgradeNodeInformationHidden(UpgradeNodeView upgradeNode)
        {
            mainScreen.UpgradeTooltipView.Hide();
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
            RefreshArtifactView();
            RefreshRecipeView();
        }

        void RefreshArtifactView()
        {
            if (mainScreen.ArtifactView == null) { return; }

            var game = ServiceLocator.Get<GameManager>().Game;
            var player = game.Player;
            mainScreen.ArtifactView.SetAvailableSelectionCount(player.AvailableArtifactSelectionCount);

            if (player.AvailableArtifactSelectionCount <= 0)
            {
                mainScreen.ArtifactView.SetArtifactList(System.Array.Empty<Data.Artifact>());
                return;
            }

            var candidates = GetArtifactCandidates(player);
            mainScreen.ArtifactView.SetArtifactList(candidates);
        }

        Data.Artifact[] GetArtifactCandidates(Model.Player player)
        {
            var artifacts = ServiceLocator.Get<DataManager>().ArtifactDataArray;
            if (artifacts == null || artifacts.Length == 0) { return System.Array.Empty<Data.Artifact>(); }

            var candidates = new System.Collections.Generic.List<Data.Artifact>();
            foreach (var artifact in artifacts)
            {
                if (artifact == null) { continue; }
                if (player.SelectedArtifactIds.Contains(artifact.Id)) { continue; }

                candidates.Add(artifact);
                if (candidates.Count >= player.RewardSelectionCount) { break; }
            }

            return candidates.ToArray();
        }

        void RefreshRecipeView()
        {
            if (mainScreen.RecipeView == null) { return; }
            if (mainScreen.RecipeView.RecipeNodes == null) { return; }

            var unlockedCount = 0;
            foreach (var node in mainScreen.RecipeView.RecipeNodes)
            {
                if (node == null) { continue; }
                node.Refresh();
                if (node.State == RecipeNode.RecipeState.Purchased)
                {
                    unlockedCount++;
                }
            }

            mainScreen.RecipeView.SetUnlockedCount(unlockedCount, mainScreen.RecipeView.RecipeNodes.Length);
        }

        public void ResetView()
        {
            var upgradeNodes = mainScreen.UpgradePanel.UpgradeTreeView.upgradeNodes;
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.Clear();
            }

            mainScreen.MenuView.Hide();
        }
    }
}
