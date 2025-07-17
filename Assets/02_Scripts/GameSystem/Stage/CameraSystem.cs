using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class CameraSystem : SystemBase
    {
        [SerializeField] TileIndicationSystem tileIndicationSystem;
        [SerializeField] Camera mainCamera;
        [SerializeField] float screenPadding = 10f;
        [SerializeField] float cameraMoveSpeed = 10f; // 카메라 이동 속도

        Model.Player player;
        Model.Map map;

        Vector2 dragStartPosition;
        Vector3 initialCameraPosition;
        bool isDragging;
        Vector2 pointerInScreen;

        public override void SetUp()
        {
            player = ServiceLocator.Get<GameManager>().Game.Player;
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

            mainCamera.transform.position = new Vector3(0f, 0f, mainCamera.transform.position.z);

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnSpecialPointerDownEvent -= OnSpecialPointerDown;
            inputManager.OnPointerMoveEvent -= OnPointerMove;
            inputManager.OnSpecialPointerUpEvent -= OnSpecialPointerUp;

            map = null;
            player = null;
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

            this.pointerInScreen = pointerInScreen;
        }

        public void UpdateCamera()
        {
            if (pointerInScreen.x < screenPadding || pointerInScreen.x > Screen.width - screenPadding ||
                pointerInScreen.y < screenPadding || pointerInScreen.y > Screen.height - screenPadding)
            {
                var screenWorldHeight = mainCamera.orthographicSize * 2f;
                var screenWorldWidth = screenWorldHeight * mainCamera.aspect;

                var cameraPosition = mainCamera.transform.position;
                Vector3 moveDirection = Vector3.zero;
                var isMoving = false;

                if (map.Size.x <= screenWorldWidth)
                {
                    cameraPosition.x = map.Size.x / 2f;
                }
                else
                {
                    if (pointerInScreen.x < screenPadding) moveDirection.x = -cameraMoveSpeed;
                    if (pointerInScreen.x > Screen.width - screenPadding) moveDirection.x = cameraMoveSpeed;
                    isMoving = true;
                }

                if (map.Size.y <= screenWorldHeight)
                {
                    cameraPosition.y = map.Size.y / 2f;
                }
                else
                {
                    if (pointerInScreen.y < screenPadding) moveDirection.y = -cameraMoveSpeed;
                    if (pointerInScreen.y > Screen.height - screenPadding) moveDirection.y = cameraMoveSpeed;
                    isMoving = true;
                }

                if (isMoving)
                {
                    cameraPosition += moveDirection * Time.deltaTime;
                    cameraPosition.x = Mathf.Clamp(cameraPosition.x, screenWorldWidth / 2f, map.Size.x - screenWorldWidth / 2f);
                    cameraPosition.y = Mathf.Clamp(cameraPosition.y, screenWorldHeight / 2f, map.Size.y - screenWorldHeight / 2f);
                }

                mainCamera.transform.position = cameraPosition;

                var pointerInWorld = mainCamera.ScreenToWorldPoint(pointerInScreen);
                var location = Vector2Int.FloorToInt(pointerInWorld);

                tileIndicationSystem.SetLocation(location);
                player.Light.Location = location;
                map.Fog.Invalidate();
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

        public void SetCameraPosition(Vector2Int location)
        {
            if (map == null) { return; }

            var screenWorldHeight = mainCamera.orthographicSize * 2f;
            var screenWorldWidth = screenWorldHeight * mainCamera.aspect;
            var cameraPosition = new Vector3(location.x, location.y, mainCamera.transform.position.z);
            if (map.Size.x <= screenWorldWidth)
            {
                cameraPosition.x = map.Size.x / 2f;
            }
            else
            {
                cameraPosition.x = Mathf.Clamp(cameraPosition.x, screenWorldWidth / 2f, map.Size.x - screenWorldWidth / 2f);
            }
            if (map.Size.y <= screenWorldHeight)
            {
                cameraPosition.y = map.Size.y / 2f;
            }
            else
            {
                cameraPosition.y = Mathf.Clamp(cameraPosition.y, screenWorldHeight / 2f, map.Size.y - screenWorldHeight / 2f);
            }
            mainCamera.transform.position = cameraPosition;
        }
    }
}