using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Modules.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Afterlife.Modules.Progression
{
    public sealed class UpgradeTreeScreen : MonoBehaviour
    {
        [Min(.01f)] public float MinimumZoom = .6f;
        [Min(.1f)] public float MaximumZoom = 1.6f;
        public Vector2 CellSize = new Vector2(120, 100);
        private UpgradeTreeData _tree;
        private UpgradeService _service;
        private Func<string, PurchaseResult> _purchase;
        private Func<bool> _available;
        private Func<float> _balance;
        private Func<string, bool> _nodeAvailable;
        [Header("고정 화면 구성 · 프리팹 참조")]
        [SerializeField] private ProgressionBackdrop _background;
        [SerializeField] private RectTransform _content, _viewport, _burstRect;
        [SerializeField] private TreePanSurface _panSurface;
        [SerializeField] private UpgradeRootVisual _rootVisual;
        [SerializeField] private UpgradeBurstVisual _burst;
        [SerializeField] private UpgradeHoverTooltip _tooltip;
        [SerializeField] private TMP_Text _header, _gold, _zoomLabel, _legend;
        [SerializeField] private Button _closeButton, _resetButton, _extraButton;
        private readonly Dictionary<string, UpgradeNodeVisual> _nodes = new Dictionary<string, UpgradeNodeVisual>();
        private readonly List<(string Parent, string Child, Image Line)> _lines = new List<(string, string, Image)>();
        private string _hovered;
        private float _zoom = 1, _refresh;
        private bool _resetPending;
        private Vector2 _lastSize;
        private const float NodeSize = 88;

        public void Bind(UpgradeTreeData tree, UpgradeService service, TMP_FontAsset font, Func<string, PurchaseResult> purchase,
            Func<bool> available, Func<float> balance, Action close, Action extraAction = null, Func<bool> extraAvailable = null,
            Func<UpgradeNodeData, Sprite> iconResolver = null, Func<string, bool> nodeAvailable = null, string extraLabel = null)
        {
            _tree = tree; _service = service; _purchase = purchase; _available = available; _balance = balance;
            _nodeAvailable = nodeAvailable;
            if (_background == null || _viewport == null || _content == null || _panSurface == null || _rootVisual == null ||
                _burstRect == null || _burst == null || _tooltip == null || _header == null || _gold == null ||
                _zoomLabel == null || _legend == null || _closeButton == null || _resetButton == null || _extraButton == null)
                throw new InvalidOperationException("UpgradeTreeScreen prefab references are incomplete.");
            _background.Laboratory = tree.Laboratory;
            _panSurface.Content = _content; _panSurface.Screen = this;
            var low = Vector2.one * (-NodeSize / 2 - 8); var high = -low;
            foreach (var node in tree.Nodes)
            {
                Vector2 position = Vector2.Scale(node.Grid, CellSize);
                low = Vector2.Min(low, position - Vector2.one * (NodeSize / 2 + 8));
                high = Vector2.Max(high, position + Vector2.one * (NodeSize / 2 + 8));
                foreach (var parent in node.Parents.Count == 0 ? new[] { "" } : node.Parents.ToArray())
                {
                    var from = string.IsNullOrEmpty(parent) ? Vector2.zero : Vector2.Scale(tree.Nodes.First(item => item.Id == parent).Grid, CellSize);
                    var line = MakeRect("Edge." + (string.IsNullOrEmpty(parent) ? "Root" : parent) + ".To." + node.Id, _content);
                    line.SetAsFirstSibling();
                    line.anchoredPosition = (from + position) / 2; line.sizeDelta = new Vector2(Vector2.Distance(from, position), 2);
                    line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(position.y - from.y, position.x - from.x) * Mathf.Rad2Deg);
                    var image = line.gameObject.AddComponent<Image>(); image.raycastTarget = false; _lines.Add((parent, node.Id, image));
                }
            }
            _rootVisual.rectTransform.sizeDelta = tree.Laboratory ? new Vector2(28, 28) : new Vector2(24, 32);
            _rootVisual.Laboratory = tree.Laboratory;
            _burstRect.sizeDelta = new Vector2(Mathf.Max(Mathf.Abs(low.x), Mathf.Abs(high.x)) * 2 + 260,
                Mathf.Max(Mathf.Abs(low.y), Mathf.Abs(high.y)) * 2 + 260);
            foreach (var node in tree.Nodes)
            {
                string id = node.Id;
                var rect = MakeRect("UpgradeNode." + node.Id, _content); rect.anchoredPosition = Vector2.Scale(node.Grid, CellSize); rect.sizeDelta = Vector2.one * NodeSize;
                var visual = rect.gameObject.AddComponent<UpgradeNodeVisual>();
                var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = visual; StyleButton(button);
                var juice = rect.gameObject.AddComponent<UpgradeNodeJuice>();
                button.onClick.AddListener(() =>
                {
                    if (_available() && (_nodeAvailable == null || _nodeAvailable(id)) && _purchase(id) == PurchaseResult.Success)
                    {
                        juice.Play();
                        _burst.Play(rect.anchoredPosition, tree.Laboratory);
                    }
                    Refresh();
                });
                var iconRect = MakeRect("UpgradeIcon", rect); iconRect.anchoredPosition = new Vector2(0, 6); iconRect.sizeDelta = new Vector2(64, 64);
                var icon = iconRect.gameObject.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
                icon.sprite = node.Icon != null ? node.Icon : iconResolver?.Invoke(node); icon.enabled = icon.sprite != null;
                visual.Icon = icon; _nodes.Add(id, visual);
                var badgeRect = MakeRect("StateBadge", rect); Stretch(badgeRect);
                var badge = badgeRect.gameObject.AddComponent<UpgradeNodeVisual>(); badge.BadgeOnly = true; badge.raycastTarget = false; visual.Badge = badge;
                var hover = rect.gameObject.AddComponent<UpgradeNodeHover>();
                hover.Enter = data => { _hovered = id; RefreshTooltip(); _tooltip.Place(data.position, data.enterEventCamera); };
                hover.Move = data => { if (_hovered == id) _tooltip.Place(data.position, data.enterEventCamera); };
                hover.Exit = HideTooltip;
            }
            _header.font = _gold.font = _zoomLabel.font = _legend.font = font;
            _header.text = tree.Title + " · 강화 트리";
            _closeButton.onClick.AddListener(() => { HideTooltip(); close(); });
            _resetButton.onClick.AddListener(ResetView);
            _extraButton.gameObject.SetActive(extraAction != null);
            if (extraAction != null)
            {
                var extraText = _extraButton.GetComponentInChildren<TMP_Text>(true);
                if (extraText != null && !string.IsNullOrEmpty(extraLabel)) extraText.text = extraLabel;
                _extraButton.onClick.AddListener(() => { HideTooltip(); extraAction(); });
                _extraButton.GetComponent<ProgressionButtonGate>().Available = extraAvailable;
                _legend.rectTransform.anchoredPosition = new Vector2(-45, 48);
                _legend.rectTransform.sizeDelta = new Vector2(370, 20);
            }
            _tooltip.Initialize((RectTransform)transform, font);
            _resetPending = true; Refresh();
        }
        private void OnEnable() { _resetPending = true; }
        private void OnDisable() { _hovered = null; if (_tooltip != null) _tooltip.HideImmediate(); }
        private void Update()
        {
            if (_service == null) return;
            var group = GetComponentInParent<CanvasGroup>(); if (group != null && !group.interactable) HideTooltip();
            _refresh -= Time.unscaledDeltaTime; if (_refresh <= 0) { _refresh = .1f; Refresh(); }
        }
        private void LateUpdate()
        {
            if (_viewport == null) return;
            if (_resetPending || _lastSize != _viewport.rect.size) { ResetView(); _resetPending = false; _lastSize = _viewport.rect.size; }
        }
        private void Refresh()
        {
            foreach (var node in _tree.Nodes)
            {
                var visual = _nodes[node.Id]; visual.gameObject.SetActive(_service.Visible(node.Id));
                visual.SetState(_service.Level(node.Id), node.Levels.Count, !_service.Unlocked(node.Id) || _nodeAvailable != null && !_nodeAvailable(node.Id));
            }
            foreach (var edge in _lines)
            {
                edge.Line.gameObject.SetActive(_service.Visible(edge.Child));
                edge.Line.color = string.IsNullOrEmpty(edge.Parent) || _service.Level(edge.Parent) > 0 ? new Color(.48f, .84f, .77f, .85f) : new Color(.4f, .44f, .52f, .4f);
            }
            _gold.text = "에테론 " + _balance().ToString("N0");
            _zoomLabel.text = "드래그 · 휠  " + (_zoom * 100).ToString("0") + "%";
            RefreshTooltip();
        }
        private void RefreshTooltip()
        {
            if (_tooltip == null) return;
            var node = _tree.Nodes.Find(item => item.Id == _hovered); if (node == null) return;
            int level = _service.Level(node.Id); bool max = level >= node.Levels.Count;
            int cost = max ? 0 : _service.NextCost(node.Id);
            bool canBuy = !max && _service.Unlocked(node.Id) && _available() && (_nodeAvailable == null || _nodeAvailable(node.Id)) && _balance() >= cost;
            _tooltip.Show(node.Title, node.Description, node.FormatValue(level), max ? null : node.FormatValue(level + 1), cost, canBuy);
        }
        public void HideTooltip() { _hovered = null; if (_tooltip != null) _tooltip.Hide(); }
        public void ZoomAt(Vector2 point, float wheel)
        {
            HideTooltip();
            float min = Mathf.Clamp(MinimumZoom, .01f, 1);
            float next = Mathf.Clamp(_zoom * Mathf.Pow(1.12f, wheel), min, Mathf.Max(1, MaximumZoom));
            _content.anchoredPosition = point - (point - _content.anchoredPosition) * (next / _zoom);
            _zoom = next; _content.localScale = Vector3.one * next;
        }
        public void ResetView()
        {
            if (_viewport == null) return; HideTooltip();
            _zoom = 1; _content.localScale = Vector3.one;
            // 루트 노드는 콘텐츠 원점에 있으므로 뷰포트 중앙에 원점을 맞춘다.
            _content.anchoredPosition = Vector2.zero;
            if (_zoomLabel != null) _zoomLabel.text = "드래그 · 휠  100%";
        }
        public static RectTransform MakeRect(string name, Transform parent)
        { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.gameObject.layer = parent.gameObject.layer; return rect; }
        public static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void StyleButton(Button button)
        {
            var colors = button.colors; colors.highlightedColor = new Color(.78f, 1, .95f); colors.pressedColor = new Color(.48f, .82f, .73f); colors.fadeDuration = .1f; button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }
    }
    public sealed class UpgradeNodeHover : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler
    {
        public Action<PointerEventData> Enter, Move;
        public Action Exit;
        public void OnPointerEnter(PointerEventData data) => Enter?.Invoke(data);
        public void OnPointerMove(PointerEventData data) => Move?.Invoke(data);
        public void OnPointerExit(PointerEventData data) => Exit?.Invoke();
        public void OnBeginDrag(PointerEventData data) { data.eligibleForClick = false; Exit?.Invoke(); }
        public void OnDrag(PointerEventData data) { }
        private void OnDisable() => Exit?.Invoke();
    }
}
