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

            isDragging = false;
        }

        public override void TearDown()
        {
            isDragging = false;

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnSpecialPointerDownEvent -= OnSpecialPointerDown;
            inputManager.OnPointerMoveEvent -= OnPointerMove;
            inputManager.OnSpecialPointerUpEvent -= OnSpecialPointerUp;

            map = null;
        }

        void OnSpecialPointerDown(Vector2 pointerInScreen)
        {
            dragStartPosition = pointerInScreen;
            initialCameraPosition = mainCamera.transform.position;
            isDragging = true;
        }

        void OnPointerMove(Vector2 pointerInScreen)
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

        void OnSpecialPointerUp(Vector2 pointerInScreen)
        {
            if (isDragging)
            {
                isDragging = false;
            }
        }
    }
}