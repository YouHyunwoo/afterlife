using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Reusable live world window. SourceCamera can point to a real gameplay scene later.</summary>
    public sealed class ThresholdPortal : ThresholdPart
    {
        public Camera SourceCamera;
        public Renderer Surface;
        public Renderer Reflection;
        public Material Template;
        public int TextureSize = 768;
        private RenderTexture _texture, _previousTarget;
        private Material _material, _reflection;
        private Material _previousSurface, _previousReflection;
        private float _visibility = 1;

        public void SetVisibility(float visibility)
        {
            _visibility = Mathf.Clamp01(visibility);
            Set(_material, _visibility);
            Set(_reflection, _visibility);
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Application.isPlaying || SourceCamera == null || Surface == null || Template == null) return;
            // Match the window aspect so its world view is not stretched vertically.
            var scale = Surface.transform.lossyScale;
            float aspect = Mathf.Abs(scale.x) / Mathf.Max(.01f, Mathf.Abs(scale.y));
            _texture = new RenderTexture(Mathf.Max(64, Mathf.RoundToInt(TextureSize * aspect)), TextureSize, 24) { name = "Threshold Village Window" };
            _texture.Create(); _previousTarget = SourceCamera.targetTexture; SourceCamera.targetTexture = _texture;
            _previousSurface = Surface.sharedMaterial;
            _material = new Material(Template); _material.mainTexture = _texture; Surface.sharedMaterial = _material;
            if (Reflection != null)
            {
                _previousReflection = Reflection.sharedMaterial;
                _reflection = new Material(_material); _reflection.SetFloat("_Reflection", 1); Reflection.sharedMaterial = _reflection;
            }
            SetVisibility(_visibility);
        }
        private void LateUpdate()
        {
            if (_material == null) return;
            float visibility = _visibility;
            if (Host != null && Host.Session != null)
            {
                var state = Host.Session;
                visibility = state.Presented == ThresholdScreen.Title ? 0 : 1;
                if (state.IsMoving && state.IsCameraTravel)
                {
                    float travel = Mathf.SmoothStep(0, 1, state.Progress);
                    visibility = state.To == ThresholdScreen.Lobby ? travel : 1 - travel;
                }
            }
            Set(_material, visibility); Set(_reflection, visibility);
        }
        private void Set(Material material, float visibility)
        {
            if (material == null) return;
            material.SetFloat("_Visibility", visibility);
            material.SetFloat("_Clock", Time.unscaledTime);
            material.SetFloat("_Motion", Host != null && Host.ReducedMotion ? 0 : 1);
        }
        protected override void OnDisable()
        {
            if (Surface != null && _material != null) Surface.sharedMaterial = _previousSurface;
            if (Reflection != null && _reflection != null) Reflection.sharedMaterial = _previousReflection;
            if (SourceCamera != null && SourceCamera.targetTexture == _texture) SourceCamera.targetTexture = _previousTarget;
            if (_texture != null) { _texture.Release(); Destroy(_texture); }
            if (_material != null) Destroy(_material);
            if (_reflection != null) Destroy(_reflection);
            _texture = null; _material = null; _reflection = null;
            base.OnDisable();
        }
    }
}
