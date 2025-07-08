using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Core
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] PlayerInput playerInput;

        InputAction normalPointerButtonAction;
        InputAction pointerMoveAction;
        InputAction specialPointerButtonAction;

        public Vector2 PointerInScreen;

        public event Action<Vector2> OnNormalPointerDownEvent;
        public event Action<Vector2> OnNormalPointerUpEvent;
        public event Action<Vector2> OnPointerMoveEvent;
        public event Action<Vector2> OnSpecialPointerDownEvent;
        public event Action<Vector2> OnSpecialPointerUpEvent;

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
                OnNormalPointerDownEvent?.Invoke(PointerInScreen);
            }
            else if (context.canceled)
            {
                OnNormalPointerUpEvent?.Invoke(PointerInScreen);
            }
        }

        void OnPointerMove(InputAction.CallbackContext context)
        {
            PointerInScreen = context.ReadValue<Vector2>();
            OnPointerMoveEvent?.Invoke(PointerInScreen);
        }

        void OnSpecialPointerButton(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnSpecialPointerDownEvent?.Invoke(PointerInScreen);
            }
            else if (context.canceled)
            {
                OnSpecialPointerUpEvent?.Invoke(PointerInScreen);
            }
        }
    }
}