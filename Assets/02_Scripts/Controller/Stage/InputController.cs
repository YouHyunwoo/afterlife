using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Afterlife.Controller
{
    public class InputController : MonoBehaviour
    {
        [SerializeField] PlayerInput playerInput;
        [SerializeField] UnityEvent<Vector2Int> onPointerDownEvent;
        [SerializeField] UnityEvent<Vector2Int> onPointerUpEvent;
        [SerializeField] UnityEvent<Vector2Int> onPointerMoveEvent;

        InputAction interactAction;
        InputAction pointerMoveAction;

        public Vector2 ScreenPointer;
        public Vector2 WorldPointer;
        public Vector2Int TileIndex;

        public event Action<Vector2Int> OnPointerDownEvent;
        public event Action<Vector2Int> OnPointerUpEvent;
        public event Action<Vector2Int> OnPointerMoveEvent;

        void Awake()
        {
            interactAction = playerInput.actions["Pointer Button"];
            pointerMoveAction = playerInput.actions["Pointer Move"];
        }

        void OnEnable()
        {
            interactAction.performed += OnPointerButton;
            interactAction.canceled += OnPointerButton;
            pointerMoveAction.performed += OnPointerMove;
        }

        void OnDisable()
        {
            interactAction.performed -= OnPointerButton;
            interactAction.canceled -= OnPointerButton;
            pointerMoveAction.performed -= OnPointerMove;
        }

        void OnPointerButton(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                onPointerDownEvent?.Invoke(TileIndex);
                OnPointerDownEvent?.Invoke(TileIndex);
            }
            else if (context.canceled)
            {
                onPointerUpEvent?.Invoke(TileIndex);
                OnPointerUpEvent?.Invoke(TileIndex);
            }
        }

        void OnPointerMove(InputAction.CallbackContext context)
        {
            ScreenPointer = context.ReadValue<Vector2>();
            WorldPointer = Camera.main.ScreenToWorldPoint(ScreenPointer);
            TileIndex = new Vector2Int(Mathf.FloorToInt(WorldPointer.x), Mathf.FloorToInt(WorldPointer.y));
            transform.position = new Vector3(TileIndex.x, TileIndex.y, 0f);

            onPointerMoveEvent?.Invoke(TileIndex);
            OnPointerMoveEvent?.Invoke(TileIndex);
        }
    }
}