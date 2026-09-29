using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Progression
{
    public sealed class UpgradeHoverTooltip : MonoBehaviour
    {
        private RectTransform _root, _rect;
        [Header("툴팁 프리팹 참조")]
        [SerializeField] private TMP_Text _title, _description, _values, _price;
        [SerializeField] private RectTransform _cost, _coin;
        private string _arrow;
        private Vector2 _pointer;
        private Camera _eventCamera;
        [SerializeField] private CanvasGroup _group;
        private float _progress, _fitScale = 1;
        private bool _showing;
        public void Initialize(RectTransform root, TMP_FontAsset font)
        {
            if (_title == null || _description == null || _values == null || _price == null || _cost == null || _coin == null || _group == null)
                throw new System.InvalidOperationException("UpgradeHoverTooltip prefab references are incomplete.");
            _root = root; _rect = (RectTransform)transform;
            _title.font = _description.font = _values.font = _price.font = font;
            _arrow = font != null && font.HasCharacter('→', true, true) ? "→" : "->";
            HideImmediate();
        }
        public void Show(string title, string description, string current, string next, int cost, bool canBuy)
        {
            _showing = true;
            gameObject.SetActive(true); transform.SetAsLastSibling();
            _title.text = title; _description.text = description;
            _values.text = next == null ? current : current + "  " + _arrow + "  " + next;
            _coin.gameObject.SetActive(next != null);
            _price.rectTransform.offsetMin = new Vector2(next != null ? 28 : 0, 0);
            _price.alignment = next != null ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            if (next != null)
            {
                _price.text = cost.ToString("N0");
                _price.color = canBuy ? new Color32(122, 214, 196, 255) : new Color32(222, 132, 132, 255);
                _cost.sizeDelta = new Vector2(28 + _price.GetPreferredValues(_price.text).x, 32);
            }
            else
            {
                _price.text = "강화 완료";
                _price.color = new Color32(237, 204, 121, 255);
                _cost.sizeDelta = new Vector2(300, 32);
            }
            // 작은 화면에서도 글자 크기는 유지하고 카드 전체를 같은 비율로 축소한다.
            float scale = Mathf.Clamp(Mathf.Min((_root.rect.width - 24) / (340 * 1.16f), (_root.rect.height - 24) / (260 * 1.16f)), .1f, 1);
            _fitScale = scale;
            ApplyAnimation();
            Place(_pointer, _eventCamera);
        }
        public void Place(Vector2 screenPoint, Camera eventCamera)
        {
            _pointer = screenPoint; _eventCamera = eventCamera;
            if (!gameObject.activeSelf) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPoint, eventCamera, out var point);
            var bounds = _root.rect;
            // 바운스/회전 중에도 프레임이 화면 밖으로 나가지 않도록 여유를 확보한다.
            var size = _rect.sizeDelta * (_fitScale * 1.16f);
            float x = point.x + 20;
            if (x + size.x > bounds.xMax - 12) x = point.x - size.x - 20;
            float y = point.y + 18;
            _rect.anchoredPosition = new Vector2(Mathf.Clamp(x + size.x / 2, bounds.xMin + size.x / 2 + 12, bounds.xMax - size.x / 2 - 12), Mathf.Clamp(y - size.y / 2, bounds.yMin + size.y / 2 + 12, bounds.yMax - size.y / 2 - 12));
        }
        public void Hide() { _showing = false; }
        public void HideImmediate() { _showing = false; _progress = 0; gameObject.SetActive(false); }
        private void OnDisable() { _showing = false; _progress = 0; }
        private void Update()
        {
            float duration = _showing ? .36f : .14f;
            _progress = Mathf.MoveTowards(_progress, _showing ? 1 : 0, Time.unscaledDeltaTime / duration);
            ApplyAnimation();
            if (!_showing && _progress <= 0) gameObject.SetActive(false);
        }
        private void ApplyAnimation()
        {
            float t = _progress;
            float scale = 1 - .5f * Mathf.Exp(-5 * t) * Mathf.Cos(t * Mathf.PI * 3.5f);
            float angle = 7 * Mathf.Sin(t * Mathf.PI * 4) * Mathf.Pow(1 - t, 2);
            _rect.localScale = Vector3.one * (_fitScale * scale);
            _rect.localRotation = Quaternion.Euler(0, 0, angle);
            _group.alpha = 1;
        }
    }
}
