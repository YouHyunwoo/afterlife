using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Captures only the assigned world camera. UI is drawn afterwards and always remains crisp.</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class ThresholdVeil : ThresholdPart
    {
        public Camera SourceCamera;
        public Material Template;
        public const int RenderWidth = 640;
        public const int RenderHeight = 320;
        private float _previousAspect;
        private RenderTexture _texture;
        private RenderTexture _previousTarget;
        private Material _material;
        private Material _previousMaterial;
        private Texture _previousTexture;
        private RawImage _image;
        private bool _previousEnabled;
        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Application.isPlaying || SourceCamera == null || Template == null) return;
            _image = GetComponent<RawImage>();
            _previousEnabled = _image.enabled;
            _image.enabled = true;
            _previousTarget = SourceCamera.targetTexture;
            _previousAspect = SourceCamera.aspect;
            _previousMaterial = _image.material; _previousTexture = _image.texture;
            _material = new Material(Template);
            _image.material = _material;
            Resize();
        }
        private void Resize()
        {
            int width = RenderWidth;
            int height = RenderHeight;
            // The overlay fills the display: compensate for its aspect to avoid stretching the world.
            SourceCamera.aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (_texture != null && _texture.width == width && _texture.height == height) return;
            ReleaseTexture();
            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "Threshold World Capture 640x320", antiAliasing = 1, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, useMipMap = false, autoGenerateMips = false };
            _texture.Create();
            SourceCamera.targetTexture = _texture;
            _image.texture = _texture;
        }
        private void LateUpdate()
        {
            if (_material == null || Host == null || Host.Session == null) return;
            Resize();
            var state = Host.Session;
            float cover = 0, blur = 0;
            if (state.IsMoving && !state.IsCameraTravel && !state.IsResultEntry)
            {
                float p = state.Progress;
                cover = p < .43f ? Mathf.SmoothStep(0, 1, p / .43f) : p < .53f ? 1 : 1 - Mathf.SmoothStep(0, 1, (p - .53f) / .47f);
                if (state.From == ThresholdScreen.Lobby && state.To == ThresholdScreen.Playing && !Host.ReducedMotion)
                    // Cut to full black at the same instant the approach reaches its safe stop.
                    // The gameplay camera switches later, while fully hidden.
                    cover = p < ThresholdPresentationSystem.PortalArrival ? 0
                        : p < .53f ? 1 : 1 - Mathf.SmoothStep(0, 1, (p - .53f) / .47f);
                blur = p >= .5f ? (1 - Mathf.InverseLerp(.5f, .96f, p)) : p;
                if (state.From == ThresholdScreen.Lobby && state.To == ThresholdScreen.Playing && p < .5f)
                    blur = 0; // Keep a single crisp portal throughout the approach.
            }
            float result = !state.IsMoving ? (ThresholdSession.IsResult(state.Current) ? 1 : 0)
                : state.IsResultEntry ? Mathf.SmoothStep(0, 1, state.Progress)
                : ThresholdSession.IsResult(state.From) ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.44f, .54f, state.Progress)) : 0;
            bool blackout = Host.Profile != null && Host.Profile.ResultBackdrop == ThresholdResultBackdrop.Blackout;
            if (blackout) cover = Mathf.Max(cover, result);
            _material.SetFloat("_Cover", cover);
            _material.SetFloat("_Result", blackout ? 0 : result);
            _material.SetFloat("_Modal", Host.ModalAmount);
            _material.SetFloat("_Blur", Mathf.Max(blur, Host.ModalAmount * .65f));
            _material.SetFloat("_Motion", Host.ReducedMotion ? 0 : 1);
            _material.SetFloat("_Clock", Time.unscaledTime);
            _material.SetFloat("_Aspect", (float)Screen.width / Mathf.Max(1, Screen.height));
        }
        private void ReleaseTexture()
        {
            if (_texture == null) return;
            if (SourceCamera != null && SourceCamera.targetTexture == _texture) SourceCamera.targetTexture = _previousTarget;
            _texture.Release(); Destroy(_texture); _texture = null;
        }
        protected override void OnDisable()
        {
            ReleaseTexture();
            if (SourceCamera != null && _material != null) SourceCamera.aspect = _previousAspect;
            if (_image != null) { _image.material = _previousMaterial; _image.texture = _previousTexture; _image.enabled = _previousEnabled; }
            if (_material != null) Destroy(_material);
            _material = null;
            base.OnDisable();
        }
    }
}
