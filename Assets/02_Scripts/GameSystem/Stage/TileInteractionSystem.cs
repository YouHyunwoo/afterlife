using System.Collections;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class TileInteractionSystem : SystemBase
    {
        [SerializeField] PlayerModeSystem playerModeSystem;
        [SerializeField] TileIndicationSystem tileIndicationSystem;
        [SerializeField] Camera mainCamera;

        public Vector2 PointerInWorld;
        public Vector2Int Location;

        Model.Player player;
        Model.Map map;

        Coroutine interactRoutine;
        bool isPointerDown;
        Vector2Int previousLocation;

        public override void SetUp()
        {
            player = ServiceLocator.Get<GameManager>().Game.Player;
            map = ServiceLocator.Get<StageManager>().Stage.Map;

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnNormalPointerDownEvent += OnPointerDown;
            inputManager.OnNormalPointerUpEvent += OnPointerUp;
            inputManager.OnPointerMoveEvent += OnPointerMove;

            enabled = true;
        }

        public override void TearDown()
        {
            isPointerDown = false;
            if (interactRoutine != null) { StopCoroutine(interactRoutine); interactRoutine = null; }

            enabled = false;

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnNormalPointerDownEvent -= OnPointerDown;
            inputManager.OnNormalPointerUpEvent -= OnPointerUp;
            inputManager.OnPointerMoveEvent -= OnPointerMove;

            player = null;
            map = null;
        }

        void OnPointerDown(Vector2 pointerInScreen)
        {
            if (!enabled) { return; }
            if (playerModeSystem.CurrentMode != EPlayerMode.Interaction) { return; }

            PointerInWorld = ConvertToPointerInWorld(pointerInScreen);
            Location = ConvertToLocation(PointerInWorld);

            isPointerDown = true;
            if (interactRoutine != null) { StopCoroutine(interactRoutine); interactRoutine = null; }
            interactRoutine = StartCoroutine(InteractRoutine(Location));
        }

        IEnumerator InteractRoutine(Vector2Int location)
        {
            InteractByLocation(location);
            if (!enabled || !player.IsAutomationEnabled) { yield break; }

            var waitTime = new WaitForSeconds(1f / player.AttackSpeed);
            var playerAttackSpeed = player.AttackSpeed;
            while (enabled && isPointerDown)
            {
                if (playerAttackSpeed != player.AttackSpeed)
                {
                    playerAttackSpeed = player.AttackSpeed;
                    waitTime = new WaitForSeconds(1f / player.AttackSpeed);
                }
                yield return waitTime;
                InteractByLocation(Location);
            }
        }

        void InteractByLocation(Vector2Int location)
        {
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
                    }
                }
            }
        }

        void OnPointerUp(Vector2 pointerInScreen)
        {
            if (!enabled) { return; }

            isPointerDown = false;
            if (interactRoutine != null) { StopCoroutine(interactRoutine); interactRoutine = null; }
        }

        void OnPointerMove(Vector2 pointerInScreen)
        {
            if (!enabled) { return; }

            PointerInWorld = ConvertToPointerInWorld(pointerInScreen);
            Location = ConvertToLocation(PointerInWorld);
            if (previousLocation == Location) { return; }
            previousLocation = Location;

            tileIndicationSystem.SetLocation(Location);

            player.Light.Location = Location;
            map.Fog.Invalidate();
            map.Fog.Update();
        }

        Vector2 ConvertToPointerInWorld(Vector2 pointerInScreen)
        {
            return mainCamera.ScreenToWorldPoint(pointerInScreen);
        }

        Vector2Int ConvertToLocation(Vector2 pointerInWorld)
        {
            return Vector2Int.FloorToInt(pointerInWorld);
        }
    }
}