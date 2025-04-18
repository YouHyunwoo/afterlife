using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.View
{
    public class TerrainTileIndicator : MonoBehaviour
    {
        [SerializeField] PlayerInput playerInput;

        InputAction interactAction;
        InputAction pointerMoveAction;

        public Vector2 ScreenPointer;
        public Vector2 WorldPointer;
        public Vector2Int TileIndex;

        public event Action<Vector2Int> OnMovedEvent;
        public event Action<Vector2Int> OnInteractedEvent;

        void Awake()
        {
            interactAction = playerInput.actions["Interact"];
            pointerMoveAction = playerInput.actions["Pointer Move"];
        }

        void OnEnable()
        {
            interactAction.performed += OnInteract;
            pointerMoveAction.performed += OnPointerMove;
        }

        void OnDisable()
        {
            interactAction.performed -= OnInteract;
            pointerMoveAction.performed -= OnPointerMove;
        }

        void OnInteract(InputAction.CallbackContext context)
        {
            OnInteractedEvent?.Invoke(TileIndex);
        }

        void OnPointerMove(InputAction.CallbackContext context)
        {
            ScreenPointer = context.ReadValue<Vector2>();
            WorldPointer = Camera.main.ScreenToWorldPoint(ScreenPointer);
            TileIndex = new Vector2Int(Mathf.FloorToInt(WorldPointer.x), Mathf.FloorToInt(WorldPointer.y));
            transform.position = new Vector3(TileIndex.x, TileIndex.y, 0f);

            OnMovedEvent?.Invoke(TileIndex);
        }
    }
}