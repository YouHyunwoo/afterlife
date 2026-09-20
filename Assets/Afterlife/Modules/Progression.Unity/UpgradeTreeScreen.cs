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
        private TMP_FontAsset _font;
        private RectTransform _content, _viewport;
        private UpgradeHoverTooltip _tooltip;
        private TMP_Text _gold, _zoomLabel;
        private readonly Dictionary<string, UpgradeNodeVisual> _nodes = new Dictionary<string, UpgradeNodeVisual>();
        private readonly List<(string Parent, string Child, Image Line)> _lines = new List<(string, string, Image)>();
        private string _hovered;
        private float _zoom = 1, _refresh;
        private bool _resetPending;
        private Vector2 _lastSize;
        private const float NodeSize = 88;

        public void Bind(UpgradeTreeData tree, UpgradeService service, TMP_FontAsset font, Func<string, PurchaseResult> purchase,
            Func<bool> available, Func<float> balance, Action close, Action extraAction = null, Func<bool> extraAvailable = null,
            Func<UpgradeNodeData, Sprite> iconResolver = null)
        {
            _tree = tree; _service = service; _font = font; _purchase = purchase; _available = available; _balance = balance;
            var root = (RectTransform)transform;
            var background = MakeRect("AmbientBackground", root); Stretch(background);
            // 애니메이션 배경의 메시 갱신이 정적인 트리 Canvas 배치까지 갱신하지 않게 분리한다.
            background.gameObject.AddComponent<Canvas>();
            background.gameObject.AddComponent<ProgressionBackdrop>().Laboratory = tree.Laboratory;
            _viewport = MakeRect("TreeViewport", root); Stretch(_viewport);
            _viewport.gameObject.AddComponent<Image>().color = Color.clear;
            _viewport.gameObject.AddComponent<RectMask2D>();
            _content = MakeRect("TreeContent", _viewport); _content.sizeDelta = Vector2.zero;
            var input = _viewport.gameObject.AddComponent<TreePanSurface>(); input.Content = _content; input.Screen = this;
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
                    line.anchoredPosition = (from + position) / 2; line.sizeDelta = new Vector2(Vector2.Distance(from, position), 2);
                    line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(position.y - from.y, position.x - from.x) * Mathf.Rad2Deg);
                    var image = line.gameObject.AddComponent<Image>(); image.raycastTarget = false; _lines.Add((parent, node.Id, image));
                }
            }
            var center = MakeRect("RootNode", _content);
            center.sizeDelta = tree.Laboratory ? new Vector2(28, 28) : new Vector2(24, 32);
            center.gameObject.AddComponent<UpgradeRootVisual>().Laboratory = tree.Laboratory;
            var burstRect = MakeRect("UpgradeParticles", _content);
            burstRect.sizeDelta = new Vector2(Mathf.Max(Mathf.Abs(low.x), Mathf.Abs(high.x)) * 2 + 260,
                Mathf.Max(Mathf.Abs(low.y), Mathf.Abs(high.y)) * 2 + 260);
            var burst = burstRect.gameObject.AddComponent<UpgradeBurstVisual>();
            foreach (var node in tree.Nodes)
            {
                string id = node.Id;
                var rect = MakeRect("UpgradeNode." + node.Id, _content); rect.anchoredPosition = Vector2.Scale(node.Grid, CellSize); rect.sizeDelta = Vector2.one * NodeSize;
                var visual = rect.gameObject.AddComponent<UpgradeNodeVisual>();
                var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = visual; StyleButton(button);
                var juice = rect.gameObject.AddComponent<UpgradeNodeJuice>();
                button.onClick.AddListener(() =>
                {
                    if (_available() && _purchase(id) == PurchaseResult.Success)
                    {
                        juice.Play();
                        burst.Play(rect.anchoredPosition, tree.Laboratory);
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
            var header = MakeRect("Header", root); header.anchorMin = header.anchorMax = new Vector2(0, 1); header.pivot = new Vector2(0, 1); header.anchoredPosition = new Vector2(24, -16); header.sizeDelta = new Vector2(300, 42);
            Label(header, tree.Title + " · 강화 트리", 25);
            var money = MakeRect("CurrencyBalance", root); money.anchorMin = money.anchorMax = new Vector2(.5f, 1); money.anchoredPosition = new Vector2(0, -38); money.sizeDelta = new Vector2(220, 40); _gold = Label(money, "", 21);
            var closeButton = MakeButton(root, "CloseButton", "닫기 · ESC", () => { HideTooltip(); close(); }); PinTopRight((RectTransform)closeButton.transform, new Vector2(-24, -18), new Vector2(145, 40));
            var reset = MakeButton(root, "ResetZoomButton", "100% 보기", ResetView); PinBottomRight((RectTransform)reset.transform, new Vector2(-24, 18), new Vector2(120, 36));
            var zoomRect = MakeRect("ZoomLabel", root); zoomRect.anchorMin = zoomRect.anchorMax = Vector2.zero; zoomRect.pivot = Vector2.zero; zoomRect.anchoredPosition = new Vector2(20, 15); zoomRect.sizeDelta = new Vector2(230, 32); _zoomLabel = Label(zoomRect, "", 15);
            var legend = MakeRect("StateLegend", root); legend.anchorMin = legend.anchorMax = new Vector2(.5f, 0); legend.pivot = new Vector2(.5f, 0); legend.anchoredPosition = new Vector2(0, 18); legend.sizeDelta = new Vector2(370, 32);
            Label(legend, "<color=#8795A8>□ 미강화</color>   <color=#7AD6C4>■ 강화 중</color>   <color=#EDCC79>✓ 완료</color>", 16);
            if (extraAction != null)
            {
                var extra = MakeButton(root, "RebirthReviewButton", "환생 · 기록", () => { HideTooltip(); extraAction(); }); PinBottomRight((RectTransform)extra.transform, new Vector2(-155, 18), new Vector2(120, 36));
                extra.gameObject.AddComponent<ProgressionButtonGate>().Available = extraAvailable;
                legend.anchoredPosition = new Vector2(-45, 48); legend.sizeDelta = new Vector2(370, 20);
            }
            var tooltipRoot = MakeRect("NodeTooltip", root); _tooltip = tooltipRoot.gameObject.AddComponent<UpgradeHoverTooltip>(); _tooltip.Initialize(root, font);
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
                visual.SetState(_service.Level(node.Id), node.Levels.Count, !_service.Unlocked(node.Id));
            }
            foreach (var edge in _lines)
            {
                edge.Line.gameObject.SetActive(_service.Visible(edge.Child));
                edge.Line.color = string.IsNullOrEmpty(edge.Parent) || _service.Level(edge.Parent) > 0 ? new Color(.48f, .84f, .77f, .85f) : new Color(.4f, .44f, .52f, .4f);
            }
            _gold.text = "골드 " + _balance().ToString("N0");
            _zoomLabel.text = "드래그 · 휠  " + (_zoom * 100).ToString("0") + "%";
            RefreshTooltip();
        }
        private void RefreshTooltip()
        {
            if (_tooltip == null) return;
            var node = _tree.Nodes.Find(item => item.Id == _hovered); if (node == null) return;
            int level = _service.Level(node.Id); bool max = level >= node.Levels.Count;
            int cost = max ? 0 : _service.NextCost(node.Id);
            bool canBuy = !max && _service.Unlocked(node.Id) && _available() && _balance() >= cost;
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
        private TMP_Text Label(RectTransform parent, string text, float size)
        {
            var rect = MakeRect("Label", parent); Stretch(rect);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = _font; label.fontSize = size; label.text = text; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = size; return label;
        }
        private static void StyleButton(Button button)
        {
            var colors = button.colors; colors.highlightedColor = new Color(.78f, 1, .95f); colors.pressedColor = new Color(.48f, .82f, .73f); colors.fadeDuration = .1f; button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }
        private Button MakeButton(Transform parent, string objectName, string title, Action click)
        {
            var rect = MakeRect(objectName, parent); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.09f, .14f, .20f, 1);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; StyleButton(button);
            var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.75f, .57f, .27f); outline.effectDistance = new Vector2(1, -1);
            button.onClick.AddListener(() => click?.Invoke()); Label(rect, title, 17); return button;
        }
        private static void PinTopRight(RectTransform rect, Vector2 position, Vector2 size) { rect.anchorMin = rect.anchorMax = Vector2.one; rect.pivot = Vector2.one; rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static void PinBottomRight(RectTransform rect, Vector2 position, Vector2 size) { rect.anchorMin = rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(1, 0); rect.anchoredPosition = position; rect.sizeDelta = size; }
    }
    public sealed class TreePanSurface : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        public RectTransform Content;
        public UpgradeTreeScreen Screen;
        private Vector2 _last;
        public void OnBeginDrag(PointerEventData data) { Screen.HideTooltip(); RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out _last); }
        public void OnDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out var point);
            Content.anchoredPosition += point - _last; _last = point;
        }
        public void OnScroll(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.enterEventCamera, out var point); Screen.ZoomAt(point, data.scrollDelta.y);
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
    public sealed class ProgressionButtonGate : MonoBehaviour
    {
        public Func<bool> Available;
        private void Update() { GetComponent<Button>().interactable = Available == null || Available(); }
    }
}
