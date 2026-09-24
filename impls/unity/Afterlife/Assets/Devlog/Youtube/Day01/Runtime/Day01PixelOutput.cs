using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Afterlife.Devlog.Day01
{
    // Owns the temporary render pipeline, pixel frame, and screen-space presentation.
    internal sealed class Day01PixelOutput : IDisposable
    {
        private const float Aspect = 16f / 9;
        private readonly Transform _owner;
        private readonly Camera _camera;
        private readonly RenderPipelineAsset _previousGraphics;
        private readonly RenderPipelineAsset _previousQuality;
        private readonly UniversalRenderPipelineAsset _pipeline;
        private readonly UniversalAdditionalCameraData _cameraData;
        private readonly CameraOverrideOption _previousColorOption;
        private Canvas _canvas;
        private RawImage _image;
        private Camera _displayCamera;
        private RenderTexture _texture;
        private readonly Afterlife.Modules.VisionBlur.VisionBlurRenderer _visionBlur;
        private Rect _display;

        public Rect Display => _display;
        public RenderTexture Texture => _texture;

        public Day01PixelOutput(Transform owner, Camera camera, UniversalRenderPipelineAsset pipeline)
        {
            _owner = owner;
            _camera = camera;
            _previousGraphics = GraphicsSettings.defaultRenderPipeline;
            _previousQuality = QualitySettings.renderPipeline;
            _pipeline = UnityEngine.Object.Instantiate(pipeline);
            _pipeline.msaaSampleCount = 1;
            _pipeline.supportsHDR = true;
            _pipeline.supportsCameraDepthTexture = true;
            _cameraData = camera.GetUniversalAdditionalCameraData();
            _cameraData.requiresDepthTexture = true;
            _previousColorOption = _cameraData.requiresColorOption;
            _cameraData.requiresColorTexture = true;
            GraphicsSettings.defaultRenderPipeline = _pipeline;
            QualitySettings.renderPipeline = _pipeline;
            _visionBlur = new Afterlife.Modules.VisionBlur.VisionBlurRenderer(camera);
        }

        public void Configure(bool pixelated, bool visionBlur, int pixelHeight)
        {
            bool framed = pixelated || visionBlur;
            if (_image != null) _image.texture = null;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            _visionBlur.SetFocus(false, false, Vector3.zero, 1);
            _visionBlur.SetSource(null);
            if (_texture != null)
            {
                _camera.targetTexture = null;
                _texture.Release();
                UnityEngine.Object.Destroy(_texture);
                _texture = null;
            }
            if (framed && _displayCamera == null)
            {
                var output = new GameObject("PixelDisplayCamera");
                output.transform.SetParent(_owner, false);
                _displayCamera = output.AddComponent<Camera>();
                _displayCamera.cullingMask = 0;
                _displayCamera.clearFlags = CameraClearFlags.SolidColor;
                _displayCamera.backgroundColor = Color.black;
                _displayCamera.allowHDR = false;
                _displayCamera.allowMSAA = false;
                var data = output.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = false;
                data.renderShadows = false;
                data.requiresDepthTexture = false;
                data.requiresColorTexture = false;
            }
            if (_displayCamera != null)
            {
                _displayCamera.targetDisplay = _camera.targetDisplay;
                _displayCamera.depth = _camera.depth + 1;
                _displayCamera.enabled = framed;
            }
            float height = Mathf.Min(Screen.height, Screen.width / Aspect);
            _display = new Rect((Screen.width - height * Aspect) / 2, (Screen.height - height) / 2, height * Aspect, height);
            _camera.rect = framed ? new Rect(0, 0, 1, 1)
                : new Rect(_display.x / Screen.width, _display.y / Screen.height, _display.width / Screen.width, _display.height / Screen.height);
            _camera.aspect = Aspect;
            if (!framed) return;
            int scale = pixelated ? Mathf.Max(1, Mathf.RoundToInt(_display.height / Mathf.Clamp(pixelHeight, 180, 720))) : 1;
            int frameHeight = Mathf.Max(1, Mathf.RoundToInt(_display.height / scale));
            _texture = new RenderTexture(Mathf.RoundToInt(frameHeight * Aspect), frameHeight, 24, RenderTextureFormat.DefaultHDR)
            {
                name = "Day01 Pixel Frame", filterMode = FilterMode.Point, antiAliasing = 1
            };
            _texture.Create();
            _camera.targetTexture = _texture;
            _visionBlur.SetSource(_texture);
            if (_canvas == null)
            {
                var root = new GameObject("PixelPresentation", typeof(RectTransform), typeof(Canvas));
                root.transform.SetParent(_owner, false);
                _canvas = root.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = -1000;
                var frame = new GameObject("PixelFrame", typeof(RectTransform), typeof(RawImage));
                frame.transform.SetParent(root.transform, false);
                _image = frame.GetComponent<RawImage>();
                _image.raycastTarget = false;
            }
            _canvas.targetDisplay = _camera.targetDisplay;
            _image.texture = _texture;
            _image.material = visionBlur ? _visionBlur.CompositeMaterial : null;
            _image.rectTransform.anchorMin = _image.rectTransform.anchorMax = Vector2.one * .5f;
            _image.rectTransform.anchoredPosition = Vector2.zero;
            _image.rectTransform.sizeDelta = _display.size;
            _canvas.gameObject.SetActive(true);
        }

        public void ResizeIfNeeded(bool pixelated, bool visionBlur, int pixelHeight)
        {
            float height = Mathf.Min(Screen.height, Screen.width / Aspect);
            if (Mathf.Abs(_display.height - height) > 1 ||
                Mathf.Abs(_display.center.x - Screen.width * .5f) > 1 ||
                Mathf.Abs(_display.center.y - Screen.height * .5f) > 1)
                Configure(pixelated, visionBlur, pixelHeight);
        }

        public void SetVisionBlur(bool enabled, ShowcaseLighting lighting)
        {
            _visionBlur.SetFocus(enabled, lighting != null && lighting.HasCursor,
                lighting != null ? lighting.CursorGround : Vector3.zero,
                lighting != null ? lighting.CursorRadius : 1);
            if (_image != null) _image.material = enabled ? _visionBlur.CompositeMaterial : null;
        }

        public void DrawLetterbox()
        {
            if (_texture != null) return;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, _display.y), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - _display.y, Screen.width, _display.y), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, _display.x, Screen.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - _display.x, 0, _display.x, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public void Dispose()
        {
            _visionBlur.Dispose();
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_canvas.gameObject);
                _canvas = null;
                _image = null;
            }
            if (_cameraData != null) _cameraData.requiresColorOption = _previousColorOption;
            if (_camera != null)
            {
                _camera.targetTexture = null;
                _camera.rect = new Rect(0, 0, 1, 1);
            }
            if (_texture != null)
            {
                _texture.Release();
                UnityEngine.Object.Destroy(_texture);
                _texture = null;
            }
            if (_displayCamera != null)
            {
                _displayCamera.enabled = false;
                UnityEngine.Object.Destroy(_displayCamera.gameObject);
                _displayCamera = null;
            }
            if (_pipeline == null) return;
            if (GraphicsSettings.defaultRenderPipeline == _pipeline) GraphicsSettings.defaultRenderPipeline = _previousGraphics;
            if (QualitySettings.renderPipeline == _pipeline) QualitySettings.renderPipeline = _previousQuality;
            UnityEngine.Object.Destroy(_pipeline);
        }
    }
}
