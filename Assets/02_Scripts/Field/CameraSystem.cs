using System.Linq;
using Afterlife.Dev.World;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CameraSystem : Moonstone.Ore.Local.System
    {
        [Header("Settings")]
        [SerializeField] Camera _camera;
        [SerializeField] float _screenPadding = 10f;
        [SerializeField] float _cameraMoveSpeed = 10f;
        [SerializeField] float _zoomSpeed = 3f;
        [SerializeField] float _minOrthographicSize = 7f;
        [SerializeField] float _maxOrthographicSize = 15f;

        WorldRepository _worldRepository;
        Vector2Int _mapSize;

        protected override void OnInitialize()
        {
            enabled = false;
        }


        protected override void OnSetUp()
        {
            var world = _worldRepository.FindAll().First();
            _mapSize = world.WorldMap.Size;
            enabled = true;
        }

        void Update()
        {
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

            if (_mapSize.x <= screenWorldWidth)
            {
                pos.x = _mapSize.x / 2f;
            }
            else
            {
                if (mousePos.x < _screenPadding) pos.x -= _cameraMoveSpeed * Time.deltaTime;
                if (mousePos.x > Screen.width - _screenPadding) pos.x += _cameraMoveSpeed * Time.deltaTime;
                pos.x = Mathf.Clamp(pos.x, screenWorldWidth / 2f, _mapSize.x - screenWorldWidth / 2f);
            }

            if (_mapSize.y <= screenWorldHeight)
            {
                pos.y = _mapSize.y / 2f;
            }
            else
            {
                if (mousePos.y < _screenPadding) pos.y -= _cameraMoveSpeed * Time.deltaTime;
                if (mousePos.y > Screen.height - _screenPadding) pos.y += _cameraMoveSpeed * Time.deltaTime;
                pos.y = Mathf.Clamp(pos.y, screenWorldHeight / 2f, _mapSize.y - screenWorldHeight / 2f);
            }

            _camera.transform.position = pos;
        }
    }
}
