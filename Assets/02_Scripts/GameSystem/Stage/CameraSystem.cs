using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class CameraSystem : SystemBase
    {
        [SerializeField] InputSystem inputSystem;
        [SerializeField] TileIndicationSystem tileIndicationSystem;
        [SerializeField] Camera mainCamera;
        [SerializeField] float screenPadding = 10f;
        [SerializeField] float cameraMoveSpeed = 10f;

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

            // inputSystem.OnSpecialPointerDownEvent += OnSpecialPointerDown;
            // inputSystem.OnSpecialPointerUpEvent += OnSpecialPointerUp;
            inputSystem.OnPointerMoveEvent += OnPointerMove;

            isDragging = false;
        }

        public override void TearDown()
        {
            isDragging = false;

            mainCamera.transform.position = new Vector3(0f, 0f, mainCamera.transform.position.z);

            // inputSystem.OnSpecialPointerDownEvent -= OnSpecialPointerDown;
            // inputSystem.OnSpecialPointerUpEvent -= OnSpecialPointerUp;
            inputSystem.OnPointerMoveEvent -= OnPointerMove;

            map = null;
            player = null;
        }

        // void OnSpecialPointerDown(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        // {
        //     dragStartPosition = pointerInScreen;
        //     initialCameraPosition = mainCamera.transform.position;
        //     isDragging = true;
        // }

        void OnPointerMove(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        {
            if (isDragging)
            {
                var delta = pointerInWorld - (Vector2)mainCamera.ScreenToWorldPoint(dragStartPosition);
                var cameraPosition = initialCameraPosition - (Vector3)delta;

                cameraPosition.x = Mathf.Clamp(cameraPosition.x, 0f, map.Size.x);
                cameraPosition.y = Mathf.Clamp(cameraPosition.y, 0f, map.Size.y);

                mainCamera.transform.position = cameraPosition;
            }

            this.pointerInScreen = pointerInScreen;
        }

        public override void UpdateSystem()
        {
            if (pointerInScreen.x < screenPadding || pointerInScreen.x > Screen.width - screenPadding ||
                pointerInScreen.y < screenPadding || pointerInScreen.y > Screen.height - screenPadding)
            {
                var screenWorldHeight = mainCamera.orthographicSize * 2f;
                var screenWorldWidth = screenWorldHeight * mainCamera.aspect;

                var cameraPosition = mainCamera.transform.position;
                Vector3 moveDirection = Vector3.zero;

                if (map.Size.x <= screenWorldWidth)
                {
                    cameraPosition.x = map.Size.x / 2f;
                }
                else
                {
                    if (pointerInScreen.x < screenPadding) moveDirection.x = -cameraMoveSpeed;
                    if (pointerInScreen.x > Screen.width - screenPadding) moveDirection.x = cameraMoveSpeed;
                    cameraPosition.x += moveDirection.x * Time.deltaTime;
                    cameraPosition.x = Mathf.Clamp(cameraPosition.x, screenWorldWidth / 2f, map.Size.x - screenWorldWidth / 2f);
                }

                if (map.Size.y <= screenWorldHeight)
                {
                    cameraPosition.y = map.Size.y / 2f;
                }
                else
                {
                    if (pointerInScreen.y < screenPadding) moveDirection.y = -cameraMoveSpeed;
                    if (pointerInScreen.y > Screen.height - screenPadding) moveDirection.y = cameraMoveSpeed;
                    cameraPosition.y += moveDirection.y * Time.deltaTime;
                    cameraPosition.y = Mathf.Clamp(cameraPosition.y, screenWorldHeight / 2f, map.Size.y - screenWorldHeight / 2f);
                }

                mainCamera.transform.position = cameraPosition;

                var pointerInWorld = mainCamera.ScreenToWorldPoint(pointerInScreen);
                var location = Vector2Int.FloorToInt(pointerInWorld);

                tileIndicationSystem.SetTilePosition(location);
                player.Light.Location = location;
                map.Fog.Invalidate();
            }
        }

        // void OnSpecialPointerUp(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        // {
        //     if (isDragging)
        //     {
        //         isDragging = false;
        //     }
        // }

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