using System.Collections;
using Afterlife.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Afterlife.GameSystem.Stage
{
    public class TileInteractionSystem : SystemBase
    {
        [SerializeField] PlayerModeSystem playerModeSystem;
        [SerializeField] TileIndicationSystem tileIndicationSystem;
        [SerializeField] Camera mainCamera;

        Model.Player player;
        Model.Map map;

        Coroutine interactionRoutine;
        bool isPointerDown;
        Vector2Int previousLocation;

        bool pointerDownRequested;
        Vector2Int pointerDownLocation;

        bool pointerMoveRequested;
        Vector2Int pointerMoveLocation;

        bool pointerUpRequested;

        public override void SetUp()
        {
            player = ServiceLocator.Get<GameManager>().Game.Player;
            map = ServiceLocator.Get<StageManager>().Stage.Map;

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnNormalPointerDownEvent += OnPointerDown;
            inputManager.OnNormalPointerUpEvent += OnPointerUp;
            inputManager.OnPointerMoveEvent += OnPointerMove;

            pointerDownRequested = false;

            enabled = true;
        }

        public override void TearDown()
        {
            isPointerDown = false;
            StopInteractionRoutine();

            enabled = false;

            pointerDownRequested = false;

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnNormalPointerDownEvent -= OnPointerDown;
            inputManager.OnNormalPointerUpEvent -= OnPointerUp;
            inputManager.OnPointerMoveEvent -= OnPointerMove;

            player = null;
            map = null;
        }

        void OnPointerDown(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        {
            if (!enabled) { return; }
            RequestPointerDown(location);
        }

        void OnPointerMove(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        {
            if (!enabled) { return; }
            RequestPointerMove(location);
        }

        void OnPointerUp(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        {
            if (!enabled) { return; }
            RequestPointerUp();
        }

        public void UpdateSystem()
        {
            if (pointerDownRequested)
            {
                pointerDownRequested = false;
                ProcessPointerDown();
            }

            if (pointerMoveRequested)
            {
                pointerMoveRequested = false;
                ProcessPointerMove();
            }

            if (pointerUpRequested)
            {
                pointerUpRequested = false;
                ProcessPointerUp();
            }
        }

        void RequestPointerDown(Vector2Int location)
        {
            pointerDownLocation = location;
            pointerDownRequested = true;
        }

        void ProcessPointerDown()
        {
            if (playerModeSystem.CurrentMode != EPlayerMode.Interaction) { return; }
            if (EventSystem.current == null) { return; }
            if (EventSystem.current.IsPointerOverGameObject()) { return; }

            StopInteractionRoutine();
            isPointerDown = true;
            StartInteractionRoutine(pointerDownLocation);
        }

        void RequestPointerMove(Vector2Int location)
        {
            pointerMoveLocation = location;
            pointerMoveRequested = true;
        }

        void ProcessPointerMove()
        {
            if (previousLocation == pointerMoveLocation) { return; }
            previousLocation = pointerMoveLocation;

            tileIndicationSystem.SetLocation(pointerMoveLocation);

            player.Light.Location = pointerMoveLocation;
            map.Fog.Invalidate();
        }

        void RequestPointerUp()
        {
            pointerUpRequested = true;
        }

        void ProcessPointerUp()
        {
            StopInteractionRoutine();
            isPointerDown = false;
        }

        void StartInteractionRoutine(Vector2Int location)
        {
            if (interactionRoutine != null) { return; }
            interactionRoutine = StartCoroutine(InteractionRoutine(location));
        }

        void StopInteractionRoutine()
        {
            if (interactionRoutine == null) { return; }
            StopCoroutine(interactionRoutine);
            interactionRoutine = null;
        }

        IEnumerator InteractionRoutine(Vector2Int location)
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
                InteractByLocation(location);
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
    }
}