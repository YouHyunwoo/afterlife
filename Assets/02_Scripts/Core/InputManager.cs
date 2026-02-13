using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Core
{
    public class InputManager : ManagerBase
    {
        [SerializeField] PlayerInput playerInput;

        InputAction normalPointerButtonAction;
        InputAction specialPointerButtonAction;
        InputAction pointerMoveAction;

        public event Action<InputAction.CallbackContext> OnNormalPointerButtonEvent;
        public event Action<InputAction.CallbackContext> OnSpecialPointerButtonEvent;
        public event Action<InputAction.CallbackContext> OnPointerMoveEvent;

        void Awake()
        {
            normalPointerButtonAction = playerInput.actions["Normal Pointer Button"];
            specialPointerButtonAction = playerInput.actions["Special Pointer Button"];
            pointerMoveAction = playerInput.actions["Pointer Move"];

            Cursor.lockState = CursorLockMode.Confined;
        }

        void OnEnable()
        {
            normalPointerButtonAction.performed += OnNormalPointerButton;
            normalPointerButtonAction.canceled += OnNormalPointerButton;
            specialPointerButtonAction.performed += OnSpecialPointerButton;
            specialPointerButtonAction.canceled += OnSpecialPointerButton;
            pointerMoveAction.performed += OnPointerMove;
        }

        void OnDisable()
        {
            normalPointerButtonAction.performed -= OnNormalPointerButton;
            normalPointerButtonAction.canceled -= OnNormalPointerButton;
            specialPointerButtonAction.performed -= OnSpecialPointerButton;
            specialPointerButtonAction.canceled -= OnSpecialPointerButton;
            pointerMoveAction.performed -= OnPointerMove;
        }

        void OnNormalPointerButton(InputAction.CallbackContext context) => OnNormalPointerButtonEvent?.Invoke(context);
        void OnSpecialPointerButton(InputAction.CallbackContext context) => OnSpecialPointerButtonEvent?.Invoke(context);
        void OnPointerMove(InputAction.CallbackContext context) => OnPointerMoveEvent?.Invoke(context);
    }
}