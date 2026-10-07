using TMPro;
using UnityEngine;

namespace Afterlife.Modules.HarvestFeedback
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PointerInputPresenter : MonoBehaviour
    {
        private readonly PointerInputPulse _pulse = new PointerInputPulse();
        private Canvas _canvas;
        private RectTransform _root;
        private UnityEngine.UI.Image _ring;
        private TextMeshProUGUI _label;
        private Texture2D _texture;
        private Sprite _sprite;

        public void Initialize(TMP_FontAsset font)
        {
            if (_root != null) { _label.font = font; Clear(); return; }
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) throw new System.InvalidOperationException("PointerInputPresenter requires a Canvas parent.");
            var root = new GameObject("PointerInputIndicator", typeof(RectTransform));
            _root = root.GetComponent<RectTransform>(); _root.SetParent(transform, false);
            _root.anchorMin = _root.anchorMax = Vector2.one * .5f; _root.sizeDelta = new Vector2(56, 56);
            var ring = new GameObject("InputRing", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            ring.transform.SetParent(_root, false); _ring = ring.GetComponent<UnityEngine.UI.Image>();
            _ring.rectTransform.anchorMin = Vector2.zero; _ring.rectTransform.anchorMax = Vector2.one;
            _ring.rectTransform.offsetMin = _ring.rectTransform.offsetMax = Vector2.zero; _ring.raycastTarget = false;
            const int size = 64;
            _texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _texture.name = "PointerInputRing"; _texture.hideFlags = HideFlags.DontSave;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
                float radius = new Vector2(x - 31.5f, y - 31.5f).magnitude;
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(3 - Mathf.Abs(radius - 27)));
            }
            _texture.SetPixels(pixels); _texture.Apply(false, true);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, size, size), Vector2.one * .5f);
            _sprite.hideFlags = HideFlags.DontSave; _ring.sprite = _sprite;
            var label = new GameObject("InputLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(_root, false); _label = label.GetComponent<TextMeshProUGUI>();
            _label.font = font; _label.fontSize = 24; _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.MidlineLeft; _label.raycastTarget = false;
            _label.rectTransform.anchorMin = _label.rectTransform.anchorMax = Vector2.one * .5f;
            _label.rectTransform.pivot = new Vector2(0, .5f); _label.rectTransform.anchoredPosition = new Vector2(34, -28);
            _label.rectTransform.sizeDelta = new Vector2(180, 48);
            Clear();
        }

        public void Tick(float delta, bool visible, Vector2 screenPoint, bool clickMode, bool pressed, bool held)
        {
            if (_root == null) return;
            if (!visible) { Clear(); return; }
            _pulse.Tick(delta, clickMode, pressed, held);
            _root.gameObject.SetActive(_pulse.Alpha > 0);
            if (_pulse.Alpha <= 0) return;
            var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPoint, camera, out var point);
            _root.anchoredPosition = point;
            var color = clickMode ? Color.white : new Color(1, .83f, .35f);
            color.a = _pulse.Alpha; _ring.color = _label.color = color;
            _label.text = clickMode ? "클릭" : "누르는 중";
            _ring.rectTransform.localScale = Vector3.one * (1 + .16f * _pulse.Alpha);
        }

        public void Clear() { _pulse.Reset(); if (_root != null) _root.gameObject.SetActive(false); }
        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            Release(_root != null ? _root.gameObject : null); Release(_sprite); Release(_texture);
        }
        private static void Release(Object value) { if (value == null) return; if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
