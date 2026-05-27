using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class CameraSystem : ISystem<ComponentManager>
    {
        private readonly Camera _camera;
        private readonly float _screenPadding;
        private readonly float _cameraMoveSpeed;
        private readonly float _zoomSpeed;
        private readonly float _minOrthographicSize;
        private readonly float _maxOrthographicSize;

        public CameraSystem(
            Camera camera,
            float screenPadding = 10f,
            float cameraMoveSpeed = 10f,
            float zoomSpeed = 3f,
            float minOrthographicSize = 7f,
            float maxOrthographicSize = 15f)
        {
            _camera = camera;
            _screenPadding = screenPadding;
            _cameraMoveSpeed = cameraMoveSpeed;
            _zoomSpeed = zoomSpeed;
            _minOrthographicSize = minOrthographicSize;
            _maxOrthographicSize = maxOrthographicSize;
        }

        public void Update(float deltaTime, ComponentManager cm)
        {
            var mapSize = cm.Game.World.Size;

            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                _camera.orthographicSize = Mathf.Clamp(
                    _camera.orthographicSize - scroll * _zoomSpeed,
                    _minOrthographicSize,
                    _maxOrthographicSize
                );
            }

            var screenWorldHeight = _camera.orthographicSize * 2f;
            var screenWorldWidth = screenWorldHeight * _camera.aspect;
            var pos = _camera.transform.position;
            var mousePos = Input.mousePosition;

            if (mapSize.x <= screenWorldWidth)
            {
                pos.x = mapSize.x / 2f;
            }
            else
            {
                if (mousePos.x < _screenPadding) pos.x -= _cameraMoveSpeed * deltaTime;
                if (mousePos.x > Screen.width - _screenPadding) pos.x += _cameraMoveSpeed * deltaTime;
                pos.x = Mathf.Clamp(pos.x, screenWorldWidth / 2f, mapSize.x - screenWorldWidth / 2f);
            }

            if (mapSize.y <= screenWorldHeight)
            {
                pos.y = mapSize.y / 2f;
            }
            else
            {
                if (mousePos.y < _screenPadding) pos.y -= _cameraMoveSpeed * deltaTime;
                if (mousePos.y > Screen.height - _screenPadding) pos.y += _cameraMoveSpeed * deltaTime;
                pos.y = Mathf.Clamp(pos.y, screenWorldHeight / 2f, mapSize.y - screenWorldHeight / 2f);
            }

            _camera.transform.position = pos;
        }
    }
}
