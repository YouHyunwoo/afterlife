using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class Controller : UI.Controller
    {
        Screen stageScreen;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            ServiceLocator.Get<AudioManager>().PlayBGM(SceneState.InGame);
            RefreshView();
        }

        public override void OnSceneExited(SceneState nextSceneState, UI.Controller nextScreen)
        {
            stageScreen.MenuView.Hide();
            stageScreen.SkillInformationView.Hide();
            stageScreen.InventoryView.Hide();
            stageScreen.CraftView.Hide();
            stageScreen.ItemInformationView.Hide();
            stageScreen.SettingsView.Hide();
        }

        public override void SetUp()
        {
            stageScreen = Screen as Stage.Screen;

            Localization.OnLanguageChangedEvent += stageScreen.Localize;
            stageScreen.Localize();

            stageScreen.OnMenuButtonClickedEvent += OnMenuButtonClicked;
            stageScreen.MenuView.OnContinueButtonClickedEvent += OnContinueButtonClicked;
            stageScreen.MenuView.OnSettingsButtonClickedEvent += OnSettingsButtonClicked;
            stageScreen.MenuView.OnGiveUpButtonClickedEvent += OnGiveUpButtonClicked;

            foreach (var itemSlot in stageScreen.InventoryView.ItemSlots)
            {
                itemSlot.OnInformationShowed += OnItemInformationShowed;
                itemSlot.OnInformationHidden += OnItemInformationHidden;
                itemSlot.SetItemIcon(null);
                itemSlot.SetEquippedIcon(false);
                itemSlot.SetItemCount(0);
            }
            foreach (var itemSlot in stageScreen.CraftView.ItemSlots)
            {
                itemSlot.OnInformationShowed += OnItemInformationShowed;
                itemSlot.OnInformationHidden += OnItemInformationHidden;
                itemSlot.SetItemIcon(null);
                itemSlot.SetEquippedIcon(false);
                itemSlot.SetItemCount(0);
            }
        }

        public override void TearDown()
        {
            stageScreen.OnMenuButtonClickedEvent -= OnMenuButtonClicked;
            stageScreen.MenuView.OnContinueButtonClickedEvent -= OnContinueButtonClicked;
            stageScreen.MenuView.OnSettingsButtonClickedEvent -= OnSettingsButtonClicked;
            stageScreen.MenuView.OnGiveUpButtonClickedEvent -= OnGiveUpButtonClicked;

            foreach (var itemSlot in stageScreen.InventoryView.ItemSlots)
            {
                itemSlot.OnInformationShowed -= OnItemInformationShowed;
                itemSlot.OnInformationHidden -= OnItemInformationHidden;
                itemSlot.SetItemIcon(null);
                itemSlot.SetEquippedIcon(false);
                itemSlot.SetItemCount(0);
            }
            foreach (var itemSlot in stageScreen.CraftView.ItemSlots)
            {
                itemSlot.OnInformationShowed -= OnItemInformationShowed;
                itemSlot.OnInformationHidden -= OnItemInformationHidden;
                itemSlot.SetItemIcon(null);
                itemSlot.SetEquippedIcon(false);
                itemSlot.SetItemCount(0);
            }

            Localization.OnLanguageChangedEvent -= stageScreen.Localize;

            stageScreen = null;
        }

        void OnMenuButtonClicked() => stageScreen.MenuView.Show();
        void OnContinueButtonClicked() => stageScreen.MenuView.Hide();
        void OnSettingsButtonClicked() => stageScreen.SettingsView.Show();
        void OnGiveUpButtonClicked() => ServiceLocator.Get<StageManager>().FailStage();
        void OnItemInformationShowed(ItemSlot itemSlot)
        {
            if (string.IsNullOrEmpty(itemSlot.ItemId)) { return; }

            if (!ServiceLocator.Get<DataManager>().ItemDataDictionary.TryGetValue(itemSlot.ItemId, out var itemData))
            {
                Debug.LogWarning($"Item data not found for ItemId: {itemSlot.ItemId}");
                return;
            }

            stageScreen.ItemInformationView.Show(itemData.Id, 0, itemData.CraftRequirements);
            var itemSlotRectTransform = itemSlot.GetComponent<RectTransform>();
            stageScreen.ItemInformationView.GetComponent<RectTransform>().position = itemSlotRectTransform.position + new Vector3(25, 25);
        }

        void OnItemInformationHidden(ItemSlot itemSlot)
        {
            stageScreen.ItemInformationView.Hide();
        }

        public override void RefreshView()
        {
            var gameManager = ServiceLocator.Get<GameManager>();
            var game = gameManager.Game;

            stageScreen.ExperienceView.SetAmount(game.Player.Experience);
            stageScreen.MenuView.MissionProgressView.SetProgress(game.CurrentStageIndex, game.TotalStageCount);
        }
    }
}