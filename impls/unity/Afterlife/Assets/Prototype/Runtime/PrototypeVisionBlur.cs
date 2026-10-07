using Afterlife.Modules.VisionBlur;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private VisionBlurRenderer _visionBlur;
        private Material _previousWorldMaterial;
        private bool _hasVisionBlurFocus;
        private Vector3 _visionBlurFocus;

        private void UpdateVisionBlur()
        {
            if (_sceneUi == null || _sceneUi.WorldDisplay == null || _camera == null) return;
            if (_visionBlur == null)
            {
                _previousWorldMaterial = _sceneUi.WorldDisplay.material;
                _visionBlur = new VisionBlurRenderer(_camera);
            }
            // 시야와 블러는 모드별 예외 없이 모델의 커서 활성 상태와 위치를 공유한다.
            _hasVisionBlurFocus = _model.PointerVisible;
            if (_hasVisionBlurFocus)
            {
                Point point = _model.WorkCenter;
                float height = _terrainSurface != null ? _terrainSurface.Height(point.X, point.Z) : 0;
                _visionBlurFocus = new Vector3(point.X, height, point.Z);
            }
            _visionBlur.SetSource(_pixels);
            _visionBlur.SetFocus(_settings.VisionBlur, _hasVisionBlurFocus, _visionBlurFocus, _settings.CursorVision);
            _sceneUi.WorldDisplay.material = _visionBlur.CompositeMaterial != null
                ? _visionBlur.CompositeMaterial : _previousWorldMaterial;
        }

        private void ResetVisionBlurFocus()
        {
            _hasVisionBlurFocus = false;
        }

        private void DisposeVisionBlur()
        {
            if (_visionBlur == null) return;
            if (_sceneUi != null && _sceneUi.WorldDisplay != null)
                _sceneUi.WorldDisplay.material = _previousWorldMaterial;
            _visionBlur.Dispose();
            _visionBlur = null;
            _hasVisionBlurFocus = false;
        }

        private void OnDisable() => DisposeVisionBlur();
    }
}
