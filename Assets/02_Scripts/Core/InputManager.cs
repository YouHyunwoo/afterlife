using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Core
{
    public class InputManager : ManagerBase
    {
        [SerializeField] PlayerInput playerInput;
        [SerializeField] Camera mainCamera;

        InputAction normalPointerButtonAction;
        InputAction pointerMoveAction;
        InputAction specialPointerButtonAction;

        public Vector2 PointerInScreen;
        public Vector2 PointerInWorld;
        public Vector2Int Location;

        public event Action<Vector2, Vector2, Vector2Int> OnNormalPointerDownEvent;
        public event Action<Vector2, Vector2, Vector2Int> OnNormalPointerUpEvent;
        public event Action<Vector2, Vector2, Vector2Int> OnPointerMoveEvent;
        public event Action<Vector2, Vector2, Vector2Int> OnSpecialPointerDownEvent;
        public event Action<Vector2, Vector2, Vector2Int> OnSpecialPointerUpEvent;

        void Awake()
        {
            normalPointerButtonAction = playerInput.actions["Normal Pointer Button"];
            pointerMoveAction = playerInput.actions["Pointer Move"];
            specialPointerButtonAction = playerInput.actions["Special Pointer Button"];
        }

        void OnEnable()
        {
            normalPointerButtonAction.performed += OnPointerButton;
            normalPointerButtonAction.canceled += OnPointerButton;
            pointerMoveAction.performed += OnPointerMove;
            specialPointerButtonAction.performed += OnSpecialPointerButton;
            specialPointerButtonAction.canceled += OnSpecialPointerButton;
        }

        void OnDisable()
        {
            normalPointerButtonAction.performed -= OnPointerButton;
            normalPointerButtonAction.canceled -= OnPointerButton;
            pointerMoveAction.performed -= OnPointerMove;
            specialPointerButtonAction.performed -= OnSpecialPointerButton;
            specialPointerButtonAction.canceled -= OnSpecialPointerButton;
        }

        void OnPointerButton(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnNormalPointerDownEvent?.Invoke(PointerInScreen, PointerInWorld, Location);
            }
            else if (context.canceled)
            {
                OnNormalPointerUpEvent?.Invoke(PointerInScreen, PointerInWorld, Location);
            }
        }

        void OnPointerMove(InputAction.CallbackContext context)
        {
            PointerInScreen = context.ReadValue<Vector2>();
            PointerInWorld = mainCamera.ScreenToWorldPoint(PointerInScreen);
            Location = Vector2Int.FloorToInt(PointerInWorld);

            OnPointerMoveEvent?.Invoke(PointerInScreen, PointerInWorld, Location);
        }

        void OnSpecialPointerButton(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnSpecialPointerDownEvent?.Invoke(PointerInScreen, PointerInWorld, Location);
            }
            else if (context.canceled)
            {
                OnSpecialPointerUpEvent?.Invoke(PointerInScreen, PointerInWorld, Location);
            }
        }
    }
}