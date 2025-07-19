using Afterlife.Core;
using Afterlife.GameSystem.Stage;
using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class Controller : UI.Controller
    {
        [SerializeField] FocusManager focusManager;
        [SerializeField] CraftSystem craftSystem;

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

            ServiceLocator.Get<LocalizationManager>().OnLanguageChangedEvent += stageScreen.Localize;
            stageScreen.Localize();

            stageScreen.InventoryButton.onClick.AddListener(ActivateInventory);
            stageScreen.CraftButton.onClick.AddListener(ActivateCraft);
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

            ServiceLocator.Get<LocalizationManager>().OnLanguageChangedEvent -= stageScreen.Localize;

            stageScreen = null;
        }

        void OnMenuButtonClicked() => focusManager.Focus(stageScreen.MenuView);
        void OnContinueButtonClicked() => stageScreen.MenuView.Hide();
        void OnSettingsButtonClicked() => focusManager.Focus(stageScreen.SettingsView);
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

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (focusManager.IsFocused) { focusManager.Clear(); }
                else { focusManager.Focus(stageScreen.MenuView); }
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                ActivateInventory();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                ActivateCraft();
            }
        }

        public void ActivateInventory()
        {
            if (focusManager.Target == stageScreen.InventoryView)
            {
                focusManager.Clear();
            }
            else if (focusManager.Target != stageScreen.MenuView)
            {
                focusManager.Focus(stageScreen.InventoryView);
            }
        }

        public void ActivateCraft()
        {
            if (focusManager.Target == stageScreen.CraftView)
            {
                focusManager.Clear();
            }
            else if (focusManager.Target != stageScreen.MenuView)
            {
                focusManager.Focus(stageScreen.CraftView);
            }
        }

        public void RefreshInventoryView()
        {
            var inventoryView = stageScreen.InventoryView;
            // TODO: 최적화
            for (int j = 0; j < inventoryView.ItemSlots.Length; j++)
            {
                inventoryView.ItemSlots[j].ItemId = null;
                inventoryView.ItemSlots[j].SetItemIcon(null);
                inventoryView.ItemSlots[j].SetEquippedIcon(false);
                inventoryView.ItemSlots[j].SetItemCount(0);
            }

            var player = ServiceLocator.Get<GameManager>().Game.Player;
            var inventory = player.Inventory;
            var equipment = player.Equipment;
            var i = 0;
            foreach (var (itemId, itemAmount) in inventory)
            {
                var itemData = ServiceLocator.Get<DataManager>().FindItemData(itemId);
                inventoryView.ItemSlots[i].ItemId = itemId;
                inventoryView.ItemSlots[i].SetItemIcon(itemData.Icon);
                inventoryView.ItemSlots[i].SetEquippedIcon(itemData.Type == Data.ItemType.Equipment && equipment.Contains(itemId));
                inventoryView.ItemSlots[i].SetItemCount(itemAmount);
                i++;
            }
        }

        public void RefreshCraftView()
        {
            var craftView = stageScreen.CraftView;
            var craftableItemIds = ServiceLocator.Get<DataManager>().CraftableItemIds;

            for (int i = 0; i < craftableItemIds.Length && i < craftView.ItemSlots.Length; i++)
            {
                var craftableItemId = craftableItemIds[i];
                var itemData = ServiceLocator.Get<DataManager>().FindItemData(craftableItemId);
                var itemSlot = craftView.ItemSlots[i];
                itemSlot.ItemId = craftableItemId;
                itemSlot.SetItemIcon(itemData.Icon);
                itemSlot.SetLocked(!craftSystem.IsCraftable(craftableItemId));
            }
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