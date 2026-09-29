using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afterlife.Modules.VisionBlur
{
    // 카메라와 원본 프레임을 주입받는다. 게임의 시야/조명 규칙이나 UI를 변경하지 않는다.
    public sealed class VisionBlurRenderer : IDisposable
    {
        private readonly Camera _camera;
        private readonly Material _composite, _gaussian;
        private RenderTexture _source, _horizontal, _blurred;
        private bool _enabled, _disposed;
        private static readonly int _sourceId = Shader.PropertyToID("_BlurSource");
        private static readonly int _blurredId = Shader.PropertyToID("_BlurredTex");
        private static readonly int _cursorId = Shader.PropertyToID("_CursorCenter");
        private static readonly int _xAxisId = Shader.PropertyToID("_CursorXAxis");
        private static readonly int _zAxisId = Shader.PropertyToID("_CursorZAxis");
        private static readonly int _hasCursorId = Shader.PropertyToID("_HasCursor");

        public Material CompositeMaterial => _enabled && _gaussian != null ? _composite : null;

        public VisionBlurRenderer(Camera camera)
        {
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            var composite = Resources.Load<Shader>("VisionBlur");
            var gaussian = Resources.Load<Shader>("VisionBlurGaussian");
            if (composite == null || gaussian == null || !composite.isSupported || !gaussian.isSupported)
            {
                Debug.LogError("VisionBlur: supported composite and Gaussian shaders are required.");
                return;
            }
            _composite = new Material(composite) { name = "Vision Blur Composite" };
            _gaussian = new Material(gaussian) { name = "Vision Blur Gaussian" };
            RenderPipelineManager.endCameraRendering += OnCameraRendered;
        }

        public void SetSource(RenderTexture source)
        {
            if (_disposed || _source == source) return;
            ReleaseFrames();
            _source = source;
            if (_composite != null) _composite.SetTexture(_blurredId, source);
        }

        public void SetFocus(bool enabled, bool hasFocus, Vector3 ground, float radius)
        {
            if (_disposed) return;
            _enabled = enabled;
            if (!enabled) { ReleaseFrames(); return; }
            if (_composite == null) return;
            radius = Mathf.Max(.01f, radius);
            Vector3 center = _camera.WorldToViewportPoint(ground);
            Vector3 x = _camera.WorldToViewportPoint(ground + Vector3.right * radius) - center;
            Vector3 z = _camera.WorldToViewportPoint(ground + Vector3.forward * radius) - center;
            _composite.SetFloat(_hasCursorId, hasFocus ? 1 : 0);
            _composite.SetVector(_cursorId, new Vector4(center.x, center.y, 0, 0));
            _composite.SetVector(_xAxisId, new Vector4(x.x, x.y, 0, 0));
            _composite.SetVector(_zAxisId, new Vector4(z.x, z.y, 0, 0));
        }

        private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
        {
            if (_disposed || !_enabled || camera != _camera || _source == null || _gaussian == null) return;
            int height = Mathf.Min(_source.height, 360);
            int width = Mathf.Max(1, Mathf.RoundToInt(height * (_source.width / (float)_source.height)));
            if (_horizontal == null || _horizontal.width != width || _horizontal.height != _source.height ||
                _blurred == null || _blurred.height != height)
            {
                ReleaseFrames();
                // 각 축을 먼저 필터링하고 해당 축을 축소해 규칙적인 격자를 방지한다.
                _horizontal = CreateFrame(width, _source.height, "Vision Blur Horizontal");
                _blurred = CreateFrame(width, height, "Vision Blur Result");
            }
            var previous = RenderTexture.active;
            try
            {
                _gaussian.SetTexture(_sourceId, _source);
                Graphics.Blit(_source, _horizontal, _gaussian, 0);
                _gaussian.SetTexture(_sourceId, _horizontal);
                Graphics.Blit(_horizontal, _blurred, _gaussian, 1);
                _composite.SetTexture(_blurredId, _blurred);
            }
            finally { RenderTexture.active = previous; }
        }

        private static RenderTexture CreateFrame(int width, int height, string name)
        {
            var frame = new RenderTexture(width, height, 0, RenderTextureFormat.DefaultHDR)
            { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            frame.Create();
            return frame;
        }

        private void ReleaseFrames()
        {
            if (_composite != null) _composite.SetTexture(_blurredId, _source);
            Release(ref _horizontal);
            Release(ref _blurred);
        }

        private static void Release(ref RenderTexture frame)
        {
            if (frame == null) return;
            frame.Release();
            Afterlife.Modules.UnitySupport.ObjectLifetime.Release(frame);
            frame = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            RenderPipelineManager.endCameraRendering -= OnCameraRendered;
            ReleaseFrames();
            _source = null;
            if (_composite != null) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_composite);
            if (_gaussian != null) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_gaussian);
        }
    }
}
