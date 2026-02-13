using System;
using Afterlife.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.GameSystem.Stage
{
    public class InputSystem : SystemBase
    {
        [SerializeField] InputManager inputManager;
        [SerializeField] Camera mainCamera;

        public Vector2 PointerInScreen;
        public Vector2 PointerInWorld;
        public Vector2Int PointerInTile;

        bool normalPointerDownRequested;
        Vector2 normalPointerDownInScreen;
        bool normalPointerUpRequested;
        Vector2 normalPointerUpInScreen;
        bool specialPointerDownRequested;
        Vector2 specialPointerDownInScreen;
        bool specialPointerUpRequested;
        Vector2 specialPointerUpInScreen;
        bool pointerMoveRequested;
        Vector2 pointerMoveInScreen;

        public event Action<Vector2, Vector2, Vector2Int> OnNormalPointerDownEvent;
        public event Action<Vector2, Vector2, Vector2Int> OnNormalPointerUpEvent;
        public event Action<Vector2, Vector2, Vector2Int> OnSpecialPointerDownEvent;
        public event Action<Vector2, Vector2, Vector2Int> OnSpecialPointerUpEvent;
        public event Action<Vector2, Vector2, Vector2Int> OnPointerMoveEvent;

        public override void SetUp()
        {
            inputManager.OnNormalPointerButtonEvent += OnPointerButton;
            inputManager.OnSpecialPointerButtonEvent += OnSpecialPointerButton;
            inputManager.OnPointerMoveEvent += OnPointerMove;

            if (Mouse.current != null)
            {
                PointerInScreen = Mouse.current.position.ReadValue();
                PointerInWorld = mainCamera.ScreenToWorldPoint(PointerInScreen);
                PointerInTile = Vector2Int.FloorToInt(PointerInWorld);
            }

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            inputManager.OnNormalPointerButtonEvent -= OnPointerButton;
            inputManager.OnSpecialPointerButtonEvent -= OnSpecialPointerButton;
            inputManager.OnPointerMoveEvent -= OnPointerMove;
        }

        void OnPointerButton(InputAction.CallbackContext context)
        {
            if (context.performed) { RequestNormalPointerDown(); }
            else if (context.canceled) { RequestNormalPointerUp(); }
        }

        void OnSpecialPointerButton(InputAction.CallbackContext context)
        {
            if (context.performed) { RequestSpecialPointerDown(); }
            else if (context.canceled) { RequestSpecialPointerUp(); }
        }

        void OnPointerMove(InputAction.CallbackContext context)
        {
            RequestPointerMove(context.ReadValue<Vector2>());
        }

        public override void UpdateSystem()
        {
            if (normalPointerDownRequested) { ProcessPointerDown(); }
            if (normalPointerUpRequested) { ProcessPointerUp(); }
            if (specialPointerDownRequested) { ProcessSpecialPointerDown(); }
            if (specialPointerUpRequested) { ProcessSpecialPointerUp(); }
            if (pointerMoveRequested) { ProcessPointerMove(); }
        }

        void RequestNormalPointerDown()
        {
            normalPointerDownInScreen = PointerInScreen;
            normalPointerDownRequested = true;
        }
        void ProcessPointerDown()
        {
            normalPointerDownRequested = false;

            CalculateCoordinates(normalPointerDownInScreen);
            OnNormalPointerDownEvent?.Invoke(PointerInScreen, PointerInWorld, PointerInTile);
        }

        void RequestNormalPointerUp()
        {
            normalPointerUpInScreen = PointerInScreen;
            normalPointerUpRequested = true;
        }
        void ProcessPointerUp()
        {
            normalPointerUpRequested = false;

            CalculateCoordinates(normalPointerUpInScreen);
            OnNormalPointerUpEvent?.Invoke(PointerInScreen, PointerInWorld, PointerInTile);
        }

        void RequestSpecialPointerDown()
        {
            specialPointerDownInScreen = PointerInScreen;
            specialPointerDownRequested = true;
        }
        void ProcessSpecialPointerDown()
        {
            specialPointerDownRequested = false;

            CalculateCoordinates(specialPointerDownInScreen);
            OnSpecialPointerDownEvent?.Invoke(PointerInScreen, PointerInWorld, PointerInTile);
        }

        void RequestSpecialPointerUp()
        {
            specialPointerUpInScreen = PointerInScreen;
            specialPointerUpRequested = true;
        }
        void ProcessSpecialPointerUp()
        {
            specialPointerUpRequested = false;

            CalculateCoordinates(specialPointerUpInScreen);
            OnSpecialPointerUpEvent?.Invoke(PointerInScreen, PointerInWorld, PointerInTile);
        }

        void RequestPointerMove(Vector2 pointerInScreen)
        {
            pointerMoveInScreen = pointerInScreen;
            pointerMoveRequested = true;
        }
        void ProcessPointerMove()
        {
            pointerMoveRequested = false;

            CalculateCoordinates(pointerMoveInScreen);
            OnPointerMoveEvent?.Invoke(PointerInScreen, PointerInWorld, PointerInTile);
        }

        void CalculateCoordinates(Vector2 pointerInScreen)
        {
            PointerInScreen = pointerInScreen;
            PointerInWorld = mainCamera.ScreenToWorldPoint(PointerInScreen);
            PointerInTile = Vector2Int.FloorToInt(PointerInWorld);
        }
    }
}