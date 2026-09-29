using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Add to a captured-background RawImage; the owner supplies the texture and visibility.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public sealed class ThresholdScreenEdge : MonoBehaviour
    {
        [SerializeField] private Material _template;
        private UnityEngine.UI.RawImage _image;
        private Material _material;
        private Material _previousMaterial;
        private float _modalAmount;
        private float _darkness;

        public void Configure(Material template) => _template = template;

        public void Present(Texture texture, float darkness, float modalAmount)
        {
            if (_image == null) _image = GetComponent<UnityEngine.UI.RawImage>();
            if (_material == null)
            {
                if (_template == null) { Debug.LogError("Threshold edge material is missing.", this); return; }
                _previousMaterial = _image.material;
                _material = new Material(_template);
                _image.material = _material;
            }
            _image.texture = texture;
            _image.color = Color.white;
            _darkness = Mathf.Clamp01(darkness);
            _modalAmount = Mathf.Clamp01(modalAmount);
            UpdateMaterial();
        }

        private void LateUpdate() => UpdateMaterial();

        private void UpdateMaterial()
        {
            if (_material == null) return;
            _material.SetFloat("_Cover", 0);
            _material.SetFloat("_Modal", _modalAmount);
            _material.SetFloat("_Result", 0);
            _material.SetFloat("_Blur", _modalAmount * .65f);
            _material.SetFloat("_Motion", 1);
            _material.SetFloat("_Clock", Time.unscaledTime);
            _material.SetFloat("_Aspect", (float)Screen.width / Mathf.Max(1, Screen.height));
            _material.SetFloat("_Darkness", _darkness);
        }

        public void Clear()
        {
            if (_image != null && _material != null) _image.material = _previousMaterial;
            if (_material != null) Destroy(_material);
            _material = null;
            _modalAmount = _darkness = 0;
        }

        private void OnDisable() => Clear();
    }
}
