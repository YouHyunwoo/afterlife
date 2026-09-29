using UnityEngine;

namespace Afterlife.Modules.CameraDistance.Unity
{
    /// <summary>거리 모델을 카메라 위치와 직교 투영 크기에 반영한다. 진행 여부는 조립 계층에서 결정한다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class GroundCameraDistance : MonoBehaviour
    {
        [Header("Ground framing")]
        [SerializeField] private float _groundHeight;
        [SerializeField, Min(.01f)] private float _referenceDistance = 22.825424f;
        [SerializeField, Min(.01f)] private float _referenceOrthographicSize = 8.7f;

        private readonly CameraDistanceProgression _progression = new CameraDistanceProgression();
        private Camera _camera;

        public void ResetDistance(float startDistance, float maximumDistance, int territoryTiles, float worldRadius)
        {
            _progression.Reset(startDistance, maximumDistance, territoryTiles);
            Apply(worldRadius);
        }

        public void FollowTerritory(int territoryTiles, float startDistance, float maximumDistance,
            int expansionTilesToMaximum, float followSpeed, float seconds, float worldRadius)
        {
            _progression.FollowTerritory(territoryTiles, startDistance, maximumDistance,
                expansionTilesToMaximum, followSpeed, seconds);
            Apply(worldRadius);
        }

        private void Apply(float worldRadius)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            var cameraTransform = _camera.transform;
            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            var ground = new Plane(Vector3.up, new Vector3(0, _groundHeight, 0));
            if (!ground.Raycast(ray, out float currentDistance)) return;

            // 현재 화면의 지면 중심과 회전은 유지한다. 직교 카메라도 실제로 줌아웃된다.
            float distance = _progression.Distance;
            cameraTransform.position = ray.GetPoint(currentDistance) - cameraTransform.forward * distance;
            if (_camera.orthographic)
                _camera.orthographicSize = _referenceOrthographicSize * distance / Mathf.Max(.01f, _referenceDistance);
            _camera.farClipPlane = Mathf.Max(80f, distance + worldRadius * 2f);
        }
    }
}
