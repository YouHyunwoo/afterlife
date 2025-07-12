using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class CameraSystem : SystemBase
    {
        [SerializeField] Camera mainCamera;

        Model.Map map;

        Vector2 dragStartPosition;
        Vector3 initialCameraPosition;
        bool isDragging;

        public override void SetUp()
        {
            map = ServiceLocator.Get<StageManager>().Stage.Map;

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnSpecialPointerDownEvent += OnSpecialPointerDown;
            inputManager.OnPointerMoveEvent += OnPointerMove;
            inputManager.OnSpecialPointerUpEvent += OnSpecialPointerUp;

            var mapSize = map.Size;
            var targetPosition = new Vector3(mapSize.x / 2f, mapSize.y / 2f, 0f)
            {
                z = mainCamera.transform.position.z
            };
            mainCamera.transform.position = targetPosition;

            isDragging = false;
        }

        public override void TearDown()
        {
            isDragging = false;

            mainCamera.transform.position = new Vector3(0f, 0f, mainCamera.transform.position.z);

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnSpecialPointerDownEvent -= OnSpecialPointerDown;
            inputManager.OnPointerMoveEvent -= OnPointerMove;
            inputManager.OnSpecialPointerUpEvent -= OnSpecialPointerUp;

            map = null;
        }

        void OnSpecialPointerDown(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        {
            dragStartPosition = pointerInScreen;
            initialCameraPosition = mainCamera.transform.position;
            isDragging = true;
        }

        void OnPointerMove(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        {
            if (isDragging)
            {
                var delta = ConvertToPointerInWorld(pointerInScreen) - ConvertToPointerInWorld(dragStartPosition);
                var cameraPosition = initialCameraPosition - (Vector3)delta;

                cameraPosition.x = Mathf.Clamp(cameraPosition.x, 0f, map.Size.x);
                cameraPosition.y = Mathf.Clamp(cameraPosition.y, 0f, map.Size.y);

                mainCamera.transform.position = cameraPosition;
            }
        }

        Vector2 ConvertToPointerInWorld(Vector2 pointerInScreen)
        {
            return mainCamera.ScreenToWorldPoint(pointerInScreen);
        }

        void OnSpecialPointerUp(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        {
            if (isDragging)
            {
                isDragging = false;
            }
        }
    }
}