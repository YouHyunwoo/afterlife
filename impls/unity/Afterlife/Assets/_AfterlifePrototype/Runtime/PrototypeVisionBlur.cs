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
            // UI 위나 모달에서는 마지막 초점을 유지한다. 건설 모드도 커서 주변은 선명하다.
            bool hasPointer = _model.PointerVisible || (_model.Mode == InputMode.Build && _pointerValid);
            if (hasPointer && _modal == Modal.None && Application.isFocused)
            {
                Point point = _model.PointerVisible ? _model.WorkCenter : _pointer;
                float height = _terrainSurface != null ? _terrainSurface.Height(point.X, point.Z) : 0;
                _visionBlurFocus = new Vector3(point.X, height, point.Z);
                _hasVisionBlurFocus = true;
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
