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
        private static readonly int _cursorCenterId = Shader.PropertyToID("_CursorCenter");
        private static readonly int _cursorXAxisId = Shader.PropertyToID("_CursorXAxis");
        private static readonly int _cursorZAxisId = Shader.PropertyToID("_CursorZAxis");
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
        private RenderTexture _blurHorizontal, _blurred;
        private readonly Material _visionBlur;
        private readonly Material _gaussianBlur;
        private bool _blurShown;
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
            var blurShader = Resources.Load<Shader>("VisionBlur");
            if (blurShader != null) _visionBlur = new Material(blurShader) { name = "Day01 Vision Blur" };
            else Debug.LogError("Day01: VisionBlur shader is missing from Resources.");
            var gaussianShader = Resources.Load<Shader>("VisionBlurGaussian");
            if (gaussianShader != null) _gaussianBlur = new Material(gaussianShader) { name = "Day01 Gaussian Blur" };
            else Debug.LogError("Day01: VisionBlurGaussian shader is missing from Resources.");
            RenderPipelineManager.endCameraRendering += OnCameraRendered;
        }

        public void Configure(bool pixelated, bool visionBlur, int pixelHeight)
        {
            bool framed = pixelated || visionBlur;
            if (_image != null) _image.texture = null;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            ReleaseBlurFrames();
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
            if (!framed) { _blurShown = false; return; }
            int scale = pixelated ? Mathf.Max(1, Mathf.RoundToInt(_display.height / Mathf.Clamp(pixelHeight, 180, 720))) : 1;
            int frameHeight = Mathf.Max(1, Mathf.RoundToInt(_display.height / scale));
            _texture = new RenderTexture(Mathf.RoundToInt(frameHeight * Aspect), frameHeight, 24, RenderTextureFormat.DefaultHDR)
            {
                name = "Day01 Pixel Frame", filterMode = FilterMode.Point, antiAliasing = 1
            };
            _texture.Create();
            _camera.targetTexture = _texture;
            // 블러 프레임이 완성되기 전에는 원본을 사용해 검은 텍스처와 섞이지 않게 한다.
            if (_visionBlur != null) _visionBlur.SetTexture("_BlurredTex", _texture);
            if (visionBlur && _visionBlur != null && _gaussianBlur != null)
            {
                _blurHorizontal = CreateBlurFrame("Day01 Blur Horizontal", true);
                _blurred = CreateBlurFrame("Day01 Blurred Frame");
            }
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
            _image.material = visionBlur ? _visionBlur : null;
            _blurShown = visionBlur;
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
            if (_image == null || _texture == null) return;
            if (_blurShown != enabled) { _image.material = enabled ? _visionBlur : null; _blurShown = enabled; }
            if (!enabled || _visionBlur == null || lighting == null) return;
            _visionBlur.SetFloat("_HasCursor", lighting.HasCursor ? 1 : 0);
            SetVisionSource(_cursorCenterId, _cursorXAxisId, _cursorZAxisId, lighting.CursorGround, lighting.CursorRadius);
        }

        private RenderTexture CreateBlurFrame(string name, bool preserveHeight = false)
        {
            // 블러 결과는 저해상도에서 만들고 선형 확대해 격자와 GPU 비용을 줄인다.
            int height = Mathf.Min(_texture.height, 360);
            int width = Mathf.Max(1, Mathf.RoundToInt(height * (_texture.width / (float)_texture.height)));
            // 세로 축은 세로 블러가 끝날 때 축소해야 가는 무늬가 격자로 접히지 않는다.
            var frame = new RenderTexture(width, preserveHeight ? _texture.height : height, 0, RenderTextureFormat.DefaultHDR)
            { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            frame.Create();
            return frame;
        }

        private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || !_blurShown || _visionBlur == null || _gaussianBlur == null ||
                _texture == null || _blurHorizontal == null || _blurred == null) return;
            // URP의 카메라 Submit 이후 콜백이므로 별도 컨텍스트 큐에 남기지 않는다.
            // 각 패스의 입력을 명시하고 완료된 두 번째 결과만 UI에 연결한다.
            var previousTarget = RenderTexture.active;
            try
            {
                _gaussianBlur.SetTexture("_BlurSource", _texture);
                Graphics.Blit(_texture, _blurHorizontal, _gaussianBlur, 0);
                _gaussianBlur.SetTexture("_BlurSource", _blurHorizontal);
                Graphics.Blit(_blurHorizontal, _blurred, _gaussianBlur, 1);
                _visionBlur.SetTexture("_BlurredTex", _blurred);
            }
            finally { RenderTexture.active = previousTarget; }
        }

        private void ReleaseBlurFrames()
        {
            if (_visionBlur != null) _visionBlur.SetTexture("_BlurredTex", null);
            if (_blurHorizontal != null) { _blurHorizontal.Release(); UnityEngine.Object.Destroy(_blurHorizontal); _blurHorizontal = null; }
            if (_blurred != null) { _blurred.Release(); UnityEngine.Object.Destroy(_blurred); _blurred = null; }
        }

        private void SetVisionSource(int centerId, int xAxisId, int zAxisId, Vector3 ground, float radius)
        {
            Vector3 center = _camera.WorldToViewportPoint(ground);
            Vector3 x = _camera.WorldToViewportPoint(ground + Vector3.right * radius) - center;
            Vector3 z = _camera.WorldToViewportPoint(ground + Vector3.forward * radius) - center;
            _visionBlur.SetVector(centerId, new Vector4(center.x, center.y, 0, 0));
            _visionBlur.SetVector(xAxisId, new Vector4(x.x, x.y, 0, 0));
            _visionBlur.SetVector(zAxisId, new Vector4(z.x, z.y, 0, 0));
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
            RenderPipelineManager.endCameraRendering -= OnCameraRendered;
            ReleaseBlurFrames();
            if (_visionBlur != null) UnityEngine.Object.Destroy(_visionBlur);
            if (_gaussianBlur != null) UnityEngine.Object.Destroy(_gaussianBlur);
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
