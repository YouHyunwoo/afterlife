using System.Collections;
using Afterlife.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Afterlife.GameSystem.Stage
{
    public class TileInteractionSystem : SystemBase
    {
        [SerializeField] InputSystem inputSystem;
        [SerializeField] PlayerModeSystem playerModeSystem;
        [SerializeField] TileIndicationSystem tileIndicationSystem;

        Model.Player player;
        Model.Map map;

        Coroutine interactionRoutine;
        Vector2Int previousLocation;

        public override void SetUp()
        {
            player = ServiceLocator.Get<GameManager>().Game.Player;
            map = ServiceLocator.Get<StageManager>().Stage.Map;

            inputSystem.OnNormalPointerDownEvent += OnNormalPointerDown;
            inputSystem.OnNormalPointerUpEvent += OnNormalPointerUp;
            inputSystem.OnPointerMoveEvent += OnPointerMove;

            enabled = true;
        }

        public override void TearDown()
        {
            StopInteractionRoutine();

            enabled = false;

            inputSystem.OnNormalPointerDownEvent -= OnNormalPointerDown;
            inputSystem.OnNormalPointerUpEvent -= OnNormalPointerUp;
            inputSystem.OnPointerMoveEvent -= OnPointerMove;

            player = null;
            map = null;
        }

        void OnNormalPointerDown(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int pointerInTile)
        {
            if (!enabled) { return; }
            if (playerModeSystem.CurrentMode != EPlayerMode.Interaction) { return; }
            if (EventSystem.current == null) { return; }
            if (EventSystem.current.IsPointerOverGameObject()) { return; }

            StopInteractionRoutine();
            StartInteractionRoutine(pointerInTile);
        }

        void OnNormalPointerUp(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int pointerInTile)
        {
            if (!enabled) { return; }
        }

        void OnPointerMove(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int pointerInTile)
        {
            if (!enabled) { return; }
            if (previousLocation == pointerInTile) { return; }

            if (map.Field.IsInBounds(previousLocation) && map.Field.Has(previousLocation))
            {
                map.Field.SpriteRendererGrid[previousLocation.x, previousLocation.y].sortingOrder = 0;
                map.Field.TextGrid[previousLocation.x, previousLocation.y].sortingOrder = 0;
            }
            previousLocation = pointerInTile;

            tileIndicationSystem.SetTilePosition(pointerInTile);
            tileIndicationSystem.SetColor(map.Field.IsInBounds(pointerInTile) ? Color.white : Color.clear);

            if (map.Field.IsInBounds(pointerInTile) && map.Field.Has(pointerInTile))
            {
                map.Field.SpriteRendererGrid[pointerInTile.x, pointerInTile.y].sortingOrder = 99;
                map.Field.TextGrid[pointerInTile.x, pointerInTile.y].sortingOrder = 99;
            }

            player.Light.Location = pointerInTile;
            map.Fog.Invalidate();
        }

        void StartInteractionRoutine(Vector2Int tilePosition)
        {
            if (interactionRoutine != null) { return; }
            interactionRoutine = StartCoroutine(InteractionRoutine(tilePosition));
        }

        void StopInteractionRoutine()
        {
            if (interactionRoutine == null) { return; }
            StopCoroutine(interactionRoutine);
            interactionRoutine = null;
        }

        IEnumerator InteractionRoutine(Vector2Int tilePosition)
        {
            var hasTileObject = map.Field.IsInBounds(tilePosition) && map.Field.Has(tilePosition);
            var targetTileObjectTransform = hasTileObject ? map.Field.Get(tilePosition) : null;

            var interactionLocation = tilePosition;
            var targetDead = false;
            var playerAttackSpeed = player.AttackSpeed;
            var waitTime = new WaitForSeconds(1f / playerAttackSpeed);

            while (enabled && !targetDead)
            {
                InteractByLocation(interactionLocation, out targetDead);

                if (playerAttackSpeed != player.AttackSpeed)
                {
                    playerAttackSpeed = player.AttackSpeed;
                    waitTime = new WaitForSeconds(1f / playerAttackSpeed);
                }

                yield return waitTime;

                if (targetTileObjectTransform != null)
                {
                    interactionLocation = Vector2Int.FloorToInt(targetTileObjectTransform.position);
                }
            }
        }

        void InteractByLocation(Vector2Int location, out bool targetDead)
        {
            targetDead = false;
            if (map == null || map.Field == null || player == null) { return; }

            var attackRange = (int)player.AttackRange - 1;
            for (int x = -attackRange; x <= attackRange; x++)
            {
                for (int y = -attackRange; y <= attackRange; y++)
                {
                    if (map == null || map.Field == null || player == null) { return; }
                    var targetLocation = new Vector2Int(location.x + x, location.y + y);
                    if (!map.Field.IsInBounds(targetLocation)) { continue; }

                    var tileObjectTransform = map.Field.Get(targetLocation);
                    if (tileObjectTransform == null) { continue; }

                    if (tileObjectTransform.TryGetComponent(out View.Object @object))
                    {
                        @object.Interact(player);
                        targetDead = !@object.IsAlive;
                    }
                }
            }
        }
    }
}