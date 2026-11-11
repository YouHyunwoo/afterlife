using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Afterlife.Modules.HarvestFeedback
{
    // Add one component to an existing Canvas. The caller supplies work events and time.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class HarvestFeedbackPresenter : MonoBehaviour
    {
        public TMP_FontAsset Font;
        public RectTransform GoldTarget;
        public Sprite WoodIcon, StoneIcon;
        [Min(.05f)] public float TextSeconds = .85f, BurstSeconds = .25f, FlightSeconds = .55f;
        [Min(1)] public float TextRise = 58, Spread = 48, Arc = 70;
        [Min(1)] public float IconSize = 32;
        public event Action IconArrived;
        private RectTransform _layer;
        private Canvas _canvas;
        private Vector3 _goldScale = Vector3.one;
        private float _goldPulse;
        private readonly List<TextEffect> _texts = new List<TextEffect>();
        private readonly List<IconEffect> _icons = new List<IconEffect>();
        private readonly Stack<TextMeshProUGUI> _textPool = new Stack<TextMeshProUGUI>();
        private readonly Stack<UnityEngine.UI.Image> _iconPool = new Stack<UnityEngine.UI.Image>();

        private sealed class TextEffect
        { public TextMeshProUGUI View; public Vector2 Origin; public float Age; public Color Color; }
        private sealed class IconEffect
        { public UnityEngine.UI.Image View; public Vector2 Origin, Scatter; public float Age, Delay, Spin; }

        public void Initialize(TMP_FontAsset font, RectTransform goldTarget, Sprite wood, Sprite stone)
        {
            if (_layer != null) Clear();
            Font = font; GoldTarget = goldTarget; WoodIcon = wood; StoneIcon = stone;
            _goldScale = GoldTarget != null ? GoldTarget.localScale : Vector3.one;
            EnsureLayer();
        }

        private void EnsureLayer()
        {
            if (_layer != null) return;
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) throw new InvalidOperationException("HarvestFeedbackPresenter requires a Canvas parent.");
            var go = new GameObject("HarvestFeedbackLayer", typeof(RectTransform));
            _layer = go.GetComponent<RectTransform>(); _layer.SetParent(transform, false);
            _layer.anchorMin = Vector2.zero; _layer.anchorMax = Vector2.one;
            _layer.offsetMin = _layer.offsetMax = Vector2.zero;
        }

        private Vector2 LocalPoint(Vector2 screen)
        {
            EnsureLayer();
            var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, camera, out var point);
            return point;
        }

        public void ShowText(Vector2 screenPoint, string value, Color color)
        {
            Vector2 origin = LocalPoint(screenPoint);
            TextMeshProUGUI text;
            if (_textPool.Count > 0) text = _textPool.Pop();
            else
            {
                var go = new GameObject("HarvestText", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(_layer, false); text = go.GetComponent<TextMeshProUGUI>();
                text.rectTransform.sizeDelta = new Vector2(260, 60);
                text.alignment = TextAlignmentOptions.Center; text.fontSize = 30; text.raycastTarget = false;
            }
            text.font = Font; text.text = value; text.color = color;
            text.rectTransform.anchoredPosition = origin; text.rectTransform.localScale = Vector3.one * .7f;
            text.gameObject.SetActive(true);
            _texts.Add(new TextEffect { View = text, Origin = origin, Color = color });
        }

        public void ShowCollection(Vector2 screenPoint, bool stone, int amount)
            => ShowCollection(screenPoint, stone ? StoneIcon : WoodIcon, amount);

        public void ShowCollection(Vector2 screenPoint, Sprite sprite, int amount)
        {
            if (GoldTarget == null || amount <= 0) return;
            if (sprite == null) throw new InvalidOperationException("Assign a collection sprite before showing harvest icons.");
            Vector2 origin = LocalPoint(screenPoint);
            // One icon per actual harvested unit, including a partial final harvest.
            for (int i = 0; i < amount; i++)
            {
                UnityEngine.UI.Image image;
                if (_iconPool.Count > 0) image = _iconPool.Pop();
                else
                {
                    var go = new GameObject("HarvestIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    go.transform.SetParent(_layer, false); image = go.GetComponent<UnityEngine.UI.Image>();
                    image.raycastTarget = false; image.preserveAspect = true;
                }
                float angle = i * 2.399963f + _icons.Count * .7f;
                image.sprite = sprite; image.color = Color.white;
                image.rectTransform.sizeDelta = Vector2.one * Mathf.Max(1, IconSize);
                image.rectTransform.anchoredPosition = origin; image.rectTransform.localScale = Vector3.one * .65f;
                image.rectTransform.localRotation = Quaternion.identity; image.gameObject.SetActive(i == 0);
                _icons.Add(new IconEffect { View = image, Origin = origin, Delay = i * .025f,
                    Scatter = new Vector2(Mathf.Cos(angle) * Spread, 18 + Mathf.Sin(angle) * Spread * .45f), Spin = Mathf.Sin(angle) * 120 });
            }
        }

        public void Tick(float delta)
        {
            delta = Mathf.Max(0, delta);
            for (int i = _texts.Count - 1; i >= 0; i--)
            {
                var effect = _texts[i]; effect.Age += delta;
                var pose = FeedbackMotion.Text(effect.Age, TextSeconds, TextRise);
                effect.View.rectTransform.anchoredPosition = effect.Origin + new Vector2(pose.X, pose.Y);
                effect.View.rectTransform.localScale = Vector3.one * pose.Scale;
                var color = effect.Color; color.a *= pose.Alpha; effect.View.color = color;
                if (pose.Complete) { effect.View.gameObject.SetActive(false); _textPool.Push(effect.View); _texts.RemoveAt(i); }
            }
            if (_layer != null && GoldTarget != null)
            {
                Vector2 target = _layer.InverseTransformPoint(GoldTarget.TransformPoint(GoldTarget.rect.center));
                for (int i = _icons.Count - 1; i >= 0; i--)
                {
                    var effect = _icons[i]; effect.Age += delta; float age = effect.Age - effect.Delay;
                    if (age < 0) continue;
                    effect.View.gameObject.SetActive(true);
                    var pose = FeedbackMotion.Collection(age, BurstSeconds, FlightSeconds,
                        effect.Origin.x, effect.Origin.y, effect.Scatter.x, effect.Scatter.y, target.x, target.y, Arc);
                    effect.View.rectTransform.anchoredPosition = new Vector2(pose.X, pose.Y);
                    effect.View.rectTransform.localScale = Vector3.one * pose.Scale;
                    effect.View.rectTransform.localRotation = Quaternion.Euler(0, 0, effect.Spin * age);
                    if (!pose.Complete) continue;
                    effect.View.gameObject.SetActive(false); _iconPool.Push(effect.View); _icons.RemoveAt(i);
                    _goldPulse = 1; IconArrived?.Invoke();
                }
                _goldPulse = Mathf.MoveTowards(_goldPulse, 0, delta * 5);
                GoldTarget.localScale = _goldScale * (1 + .16f * _goldPulse);
            }
        }

        public void Clear()
        {
            foreach (var effect in _texts) { if (effect.View != null) { effect.View.gameObject.SetActive(false); _textPool.Push(effect.View); } }
            foreach (var effect in _icons) { if (effect.View != null) { effect.View.gameObject.SetActive(false); _iconPool.Push(effect.View); } }
            _texts.Clear(); _icons.Clear(); _goldPulse = 0;
            if (GoldTarget != null) GoldTarget.localScale = _goldScale;
        }

        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            IconArrived = null;
            if (_layer != null)
            {
                if (Application.isPlaying) Destroy(_layer.gameObject); else DestroyImmediate(_layer.gameObject);
            }
            _textPool.Clear(); _iconPool.Clear();
        }
    }
}
