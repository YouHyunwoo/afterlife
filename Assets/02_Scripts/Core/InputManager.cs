using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Core
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] PlayerInput playerInput;

        InputAction pointerButtonAction;
        InputAction pointerMoveAction;

        public Vector2 PointerInScreen;

        public event Action<Vector2> OnPointerDownEvent;
        public event Action<Vector2> OnPointerUpEvent;
        public event Action<Vector2> OnPointerMoveEvent;

        void Awake()
        {
            pointerButtonAction = playerInput.actions["Pointer Button"];
            pointerMoveAction = playerInput.actions["Pointer Move"];
        }

        void OnEnable()
        {
            pointerButtonAction.performed += OnPointerButton;
            pointerButtonAction.canceled += OnPointerButton;
            pointerMoveAction.performed += OnPointerMove;
        }

        void OnDisable()
        {
            pointerButtonAction.performed -= OnPointerButton;
            pointerButtonAction.canceled -= OnPointerButton;
            pointerMoveAction.performed -= OnPointerMove;
        }

        void OnPointerButton(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnPointerDownEvent?.Invoke(PointerInScreen);
            }
            else if (context.canceled)
            {
                OnPointerUpEvent?.Invoke(PointerInScreen);
            }
        }

        void OnPointerMove(InputAction.CallbackContext context)
        {
            PointerInScreen = context.ReadValue<Vector2>();
            OnPointerMoveEvent?.Invoke(PointerInScreen);
        }
    }
}