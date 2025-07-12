using Afterlife.Core;
using Afterlife.GameSystem.Stage.Field;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class ConstructionSystem : SystemBase
    {
        [SerializeField] PlayerModeSystem playerModeSystem;
        [SerializeField] TileIndicationSystem tileIndicationSystem;
        [SerializeField] FieldObjectSystem fieldObjectSpawner;
        [SerializeField] TileInteractionSystem tileInteractionSystem;
        [SerializeField] ItemUsageSystem itemUsageSystem;
        [SerializeField] Camera mainCamera;

        Model.Player player;
        Model.Map map;

        Data.Item itemData;
        GameObject constructionPrefab;
        GameObject previewPrefab;

        public override void SetUp()
        {
            player = ServiceLocator.Get<GameManager>().Game.Player;
            map = ServiceLocator.Get<StageManager>().Stage.Map;

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnNormalPointerDownEvent += OnPointerDown;
            inputManager.OnPointerMoveEvent += OnPointerMove;

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnNormalPointerDownEvent -= OnPointerDown;
            inputManager.OnPointerMoveEvent -= OnPointerMove;

            player = null;
            map = null;
        }

        void Update()
        {
            if (playerModeSystem.CurrentMode != EPlayerMode.Construction) { return; }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopConstruction();
            }
        }

        void OnPointerDown(Vector2 pointerInScreen)
        {
            if (!enabled) { return; }
            if (playerModeSystem.CurrentMode != EPlayerMode.Construction) { return; }

            var location = map.GetTileLocationByScreenPosition(pointerInScreen, mainCamera);
            if (!map.IsAvailable(location)) { return; }

            fieldObjectSpawner.Spawn(constructionPrefab, location);

            var inventory = player.Inventory;
            if (!inventory.HasItem(itemData.Id))
            {
                Debug.LogWarning($"Cannot construct {itemData.Id}. Item not found in inventory.");
                return;
            }

            inventory.RemoveItem(itemData.Id, 1, out var _);

            itemUsageSystem.RefreshInventoryView();

            StopConstruction();
        }

        void OnPointerMove(Vector2 pointerInScreen)
        {
            if (!enabled) { return; }
            if (playerModeSystem.CurrentMode != EPlayerMode.Construction) { return; }

            var location = map.GetTileLocationByScreenPosition(pointerInScreen, mainCamera);
            previewPrefab.transform.position = (Vector2)location;
            tileIndicationSystem.SetColor(map.IsAvailable(location) ? Color.green : Color.red);
        }

        public void StartConstruction(Data.Item itemData, GameObject constructionPrefab, GameObject previewPrefab)
        {
            var location = tileInteractionSystem.Location;

            this.itemData = itemData;
            this.constructionPrefab = constructionPrefab;
            this.previewPrefab = Instantiate(previewPrefab, (Vector2)location, Quaternion.identity);

            tileIndicationSystem.SetColor(map.IsAvailable(location) ? Color.green : Color.red);
            playerModeSystem.SetMode(EPlayerMode.Construction);
        }

        public void StopConstruction()
        {
            if (previewPrefab == null) { return; }

            constructionPrefab = null;
            Destroy(previewPrefab);
            previewPrefab = null;

            tileIndicationSystem.SetColor(Color.white);
            playerModeSystem.SetMode(EPlayerMode.Interaction);
        }
    }
}