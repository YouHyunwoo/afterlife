using Afterlife.Core;
using Afterlife.GameSystem.Stage.Field;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Afterlife.GameSystem.Stage
{
    public class ConstructionSystem : SystemBase
    {
        [SerializeField] InputSystem inputSystem;
        [SerializeField] PlayerModeSystem playerModeSystem;
        [SerializeField] TileIndicationSystem tileIndicationSystem;
        [SerializeField] FieldObjectSystem fieldObjectSpawner;
        [SerializeField] ItemUsageSystem itemUsageSystem;

        Model.Player player;
        Model.Map map;

        Vector2Int constructionLocation;
        Data.Item itemData;
        GameObject constructionPrefab;
        GameObject previewPrefab;

        public override void SetUp()
        {
            player = ServiceLocator.Get<GameManager>().Game.Player;
            map = ServiceLocator.Get<StageManager>().Stage.Map;

            inputSystem.OnNormalPointerDownEvent += OnPointerDown;
            inputSystem.OnPointerMoveEvent += OnPointerMove;

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            inputSystem.OnNormalPointerDownEvent -= OnPointerDown;
            inputSystem.OnPointerMoveEvent -= OnPointerMove;

            player = null;
            map = null;
        }

        public override void UpdateSystem()
        {
            if (!enabled) { return; }
            if (playerModeSystem.CurrentMode != EPlayerMode.Construction) { return; }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopConstruction();
                return;
            }
        }

        void OnPointerDown(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int pointerInTile)
        {
            if (!enabled) { return; }
            if (playerModeSystem.CurrentMode != EPlayerMode.Construction) { return; }
            if (EventSystem.current == null) { return; }
            if (EventSystem.current.IsPointerOverGameObject()) { return; }

            if (!map.IsAvailable(pointerInTile)) { return; }

            fieldObjectSpawner.Spawn(constructionPrefab, pointerInTile);

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

        void OnPointerMove(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int pointerInTile)
        {
            if (!enabled) { return; }
            if (playerModeSystem.CurrentMode != EPlayerMode.Construction) { return; }

            constructionLocation = pointerInTile;
            previewPrefab.transform.position = (Vector2)pointerInTile;
            tileIndicationSystem.SetColor(map.IsAvailable(pointerInTile) ? Color.green : Color.red);
        }

        public void StartConstruction(Data.Item itemData, GameObject constructionPrefab, GameObject previewPrefab)
        {
            this.itemData = itemData;
            this.constructionPrefab = constructionPrefab;
            this.previewPrefab = Instantiate(previewPrefab, (Vector2)constructionLocation, Quaternion.identity);

            tileIndicationSystem.SetColor(map.IsAvailable(constructionLocation) ? Color.green : Color.red);
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