using System;
using System.Collections.Generic;
using Afterlife.Core;
using Afterlife.Dev.UpgradeTree.Data;
using Afterlife.Dev.UpgradeTree.Model;
using Afterlife.Domain.Progression.Effect.Data;
using Afterlife.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Dev.UpgradeTree.UI
{
    public class UpgradeTreeView : Afterlife.UI.View
    {
        [Header("Data")]
        [SerializeField] private UpgradeTreeData _treeData;
        [SerializeField] private bool _buildOnAwake = true;

        [Header("Prefabs")]
        [SerializeField] private UpgradeTreeNodeView _nodePrefab;
        [SerializeField] private Image _edgePrefab;

        [Header("Containers")]
        [SerializeField] private RectTransform _nodeContainer;
        [SerializeField] private RectTransform _edgeContainer;

        [Header("Tooltip")]
        [SerializeField] private UpgradeTreeTooltipView _tooltipView;
        [SerializeField] private Vector2 _tooltipOffset = new(0f, 12f);

        [Header("Dev Test")]
        [SerializeField] private int _testExperienceGrantAmount = 1000;

        private readonly UpgradeTreeService _service = new();
        private readonly Dictionary<string, UpgradeTreeNodeView> _nodeViews = new();
        private readonly List<Image> _edgeViews = new();
        private UpgradeTreeProgress _progress = new();
        private UpgradeTreeNodeView _hoveredNodeView;
        private float _devExperience;
        private float _lastObservedExperience = float.NaN;

        public UpgradeTreeProgress Progress => _progress;
        public event Action<string, UpgradeTreePurchaseResult> OnPurchaseCompleted;

        private void Awake()
        {
            if (_buildOnAwake && _treeData != null)
            {
                Initialize(_treeData, new UpgradeTreeProgress());
            }
        }

        private void Update()
        {
            RefreshIfExperienceChanged();
        }

        public void Initialize(UpgradeTreeData treeData, UpgradeTreeProgress progress)
        {
            _treeData = treeData;
            _progress = progress ?? new UpgradeTreeProgress();
            Rebuild();
        }

        public void Initialize(UpgradeTreeProgress progress)
        {
            Initialize(_treeData, progress);
        }

        public void Rebuild()
        {
            ClearRuntimeViews();
            if (_treeData == null || _nodePrefab == null) { return; }

            var nodeParent = _nodeContainer != null ? _nodeContainer : transform as RectTransform;
            foreach (var node in _treeData.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id)) { continue; }

                var nodeView = Instantiate(_nodePrefab, nodeParent);
                nodeView.name = $"Upgrade Node - {node.Id}";
                nodeView.Bind(node);
                nodeView.OnClicked += HandleNodeClicked;
                nodeView.OnInformationShowed += HandleInformationShowed;
                nodeView.OnInformationHidden += HandleInformationHidden;

                var rectTransform = nodeView.transform as RectTransform;
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = _treeData.NodeSize;
                    rectTransform.anchoredPosition = new Vector2(
                        node.GridPosition.x * _treeData.CellSize.x,
                        -node.GridPosition.y * _treeData.CellSize.y);
                }

                _nodeViews[node.Id] = nodeView;
            }

            Refresh();
        }

        public void Refresh()
        {
            foreach (var pair in _nodeViews)
            {
                var nodeView = pair.Value;
                var node = nodeView.NodeData;
                var acquiredLevelCount = _progress.GetAcquiredLevelCount(node.Id);
                var maxLevelCount = node.Levels != null ? node.Levels.Length : 0;
                var state = _service.GetNodeState(_treeData, node, _progress);
                nodeView.Refresh(state, acquiredLevelCount, maxLevelCount, CanAffordNextLevel(node));
            }

            RebuildEdges();
        }

        public void GrantTestExperience()
        {
            AddExperience(_testExperienceGrantAmount);
        }

        public void AddExperience(float amount)
        {
            if (amount <= 0f) { return; }

            if (TryGetPlayer(out var player))
            {
                player.TakeExperience(amount);
                Debug.Log($"[Dev UpgradeTree] 경험치 +{amount}, 현재 경험치: {player.Experience}");
                RefreshExperienceDependentViews(player.Experience);
                return;
            }

            _devExperience += amount;
            Debug.Log($"[Dev UpgradeTree] 경험치 +{amount}, 현재 경험치: {_devExperience}");
            RefreshExperienceDependentViews(_devExperience);
        }

        private void ClearRuntimeViews()
        {
            foreach (var pair in _nodeViews)
            {
                if (pair.Value == null) { continue; }
                pair.Value.OnClicked -= HandleNodeClicked;
                pair.Value.OnInformationShowed -= HandleInformationShowed;
                pair.Value.OnInformationHidden -= HandleInformationHidden;
                Destroy(pair.Value.gameObject);
            }

            _nodeViews.Clear();
            _hoveredNodeView = null;

            foreach (var edgeView in _edgeViews)
            {
                if (edgeView != null) { Destroy(edgeView.gameObject); }
            }

            _edgeViews.Clear();
        }

        private void RebuildEdges()
        {
            foreach (var edgeView in _edgeViews)
            {
                if (edgeView != null) { Destroy(edgeView.gameObject); }
            }

            _edgeViews.Clear();
            if (_treeData == null || _edgePrefab == null) { return; }

            var edgeParent = _edgeContainer != null ? _edgeContainer : _nodeContainer;
            foreach (var sourceNode in _treeData.Nodes)
            {
                if (sourceNode == null || sourceNode.ChildNodeIds == null) { continue; }
                if (!_nodeViews.TryGetValue(sourceNode.Id, out var sourceView)) { continue; }
                if (!sourceView.gameObject.activeSelf) { continue; }

                foreach (var childNodeId in sourceNode.ChildNodeIds)
                {
                    if (!_nodeViews.TryGetValue(childNodeId, out var targetView)) { continue; }
                    if (!targetView.gameObject.activeSelf) { continue; }
                    CreateEdge(sourceView.transform as RectTransform, targetView.transform as RectTransform, edgeParent);
                }
            }
        }

        private void CreateEdge(RectTransform source, RectTransform target, Transform parent)
        {
            if (source == null || target == null || parent == null) { return; }

            var edge = Instantiate(_edgePrefab, parent);
            edge.raycastTarget = false;
            edge.name = $"Upgrade Edge - {source.name} -> {target.name}";
            _edgeViews.Add(edge);

            var rectTransform = edge.rectTransform;
            var start = source.anchoredPosition;
            var end = target.anchoredPosition;
            var delta = end - start;

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = start;
            rectTransform.sizeDelta = new Vector2(delta.magnitude, rectTransform.sizeDelta.y);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void HandleNodeClicked(UpgradeTreeNodeView nodeView)
        {
            if (nodeView == null || nodeView.NodeData == null) { return; }

            var result = _service.TryPurchase(
                _treeData,
                nodeView.NodeData.Id,
                _progress,
                SpendExperience,
                ApplyParameter);

            Refresh();
            RefreshTooltip();
            OnPurchaseCompleted?.Invoke(nodeView.NodeData.Id, result);
        }

        private bool SpendExperience(int cost)
        {
            if (TryGetPlayer(out var player))
            {
                return player.SpendExperience(cost);
            }

            if (cost <= 0) { return true; }
            if (_devExperience < cost)
            {
                Debug.Log($"[Dev UpgradeTree] 경험치 부족. 필요: {cost}, 현재 경험치: {_devExperience}");
                return false;
            }

            _devExperience -= cost;
            Debug.Log($"[Dev UpgradeTree] 경험치 -{cost}, 현재 경험치: {_devExperience}");
            return true;
        }

        private bool CanAffordNextLevel(UpgradeTreeNodeData node)
        {
            var nextLevel = _service.GetNextLevel(node, _progress);
            if (nextLevel == null || nextLevel.Cost <= 0) { return true; }
            return GetCurrentExperience() >= nextLevel.Cost;
        }

        private float GetCurrentExperience()
        {
            return TryGetPlayer(out var player) ? player.Experience : _devExperience;
        }

        private void RefreshIfExperienceChanged()
        {
            if (_treeData == null || _nodeViews.Count == 0) { return; }

            var currentExperience = GetCurrentExperience();
            if (Mathf.Approximately(currentExperience, _lastObservedExperience)) { return; }

            RefreshExperienceDependentViews(currentExperience);
        }

        private void RefreshExperienceDependentViews(float currentExperience)
        {
            _lastObservedExperience = currentExperience;
            Refresh();
            RefreshTooltip();
        }

        private bool TryGetPlayer(out Player player)
        {
            player = null;
            try
            {
                var gameManager = ServiceLocator.Get<GameManager>();
                player = gameManager.Game?.Player;
                return player != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ApplyParameter(PlayerEffectParameter parameter)
        {
            try
            {
                PlayerParameterApplicator.Apply(parameter);
            }
            catch (Exception)
            {
                // Dev-only tree can run in isolated scenes without the legacy service locator.
            }
        }

        private void HandleInformationShowed(UpgradeTreeNodeView nodeView)
        {
            _hoveredNodeView = nodeView;
            RefreshTooltip();
        }

        private void RefreshTooltip()
        {
            var nodeView = _hoveredNodeView;
            if (_tooltipView == null || nodeView == null || nodeView.NodeData == null) { return; }
            if (!nodeView.gameObject.activeInHierarchy)
            {
                _tooltipView.Hide();
                _hoveredNodeView = null;
                return;
            }

            var node = nodeView.NodeData;
            var acquiredLevelCount = _progress.GetAcquiredLevelCount(node.Id);
            var maxLevelCount = node.Levels != null ? node.Levels.Length : 0;
            var nextLevel = _service.GetNextLevel(node, _progress);
            var completed = _service.IsCompleted(node, _progress);
            var description = completed ? GetLastLevelDescription(node) : nextLevel?.Description;

            _tooltipView.Show(
                string.IsNullOrEmpty(node.Name) ? node.Id : node.Name,
                acquiredLevelCount,
                maxLevelCount,
                nextLevel != null ? nextLevel.Cost : 0,
                description,
                completed);
            PositionTooltip(nodeView);
        }

        private string GetLastLevelDescription(UpgradeTreeNodeData node)
        {
            if (node == null || node.Levels == null || node.Levels.Length == 0) { return string.Empty; }
            return node.Levels[^1]?.Description;
        }

        private void HandleInformationHidden(UpgradeTreeNodeView nodeView)
        {
            if (_hoveredNodeView == nodeView)
            {
                _hoveredNodeView = null;
            }

            if (_tooltipView != null)
            {
                _tooltipView.Hide();
            }
        }

        private void PositionTooltip(UpgradeTreeNodeView nodeView)
        {
            if (_tooltipView == null || nodeView == null) { return; }

            var nodeRectTransform = nodeView.transform as RectTransform;
            var tooltipRectTransform = _tooltipView.transform as RectTransform;
            if (nodeRectTransform == null || tooltipRectTransform == null) { return; }

            var canvas = _tooltipView.GetComponentInParent<Canvas>();
            var canvasRectTransform = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRectTransform == null) { return; }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRectTransform);

            var camera = GetCanvasCamera(canvas);
            var nodeWorldCorners = new Vector3[4];
            nodeRectTransform.GetWorldCorners(nodeWorldCorners);

            var nodeTopCenterWorld = (nodeWorldCorners[1] + nodeWorldCorners[2]) * 0.5f;
            var nodeTopCenterScreen = RectTransformUtility.WorldToScreenPoint(camera, nodeTopCenterWorld);
            var desiredScreenPosition = nodeTopCenterScreen + _tooltipOffset;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    desiredScreenPosition,
                    camera,
                    out var localPosition))
            {
                return;
            }

            tooltipRectTransform.pivot = new Vector2(0.5f, 0f);
            tooltipRectTransform.anchoredPosition = ClampTooltipLocalPosition(
                localPosition,
                tooltipRectTransform,
                canvasRectTransform);
        }

        private Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) { return null; }
            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private Vector2 ClampTooltipLocalPosition(
            Vector2 localPosition,
            RectTransform tooltipRectTransform,
            RectTransform canvasRectTransform)
        {
            var canvasRect = canvasRectTransform.rect;
            var tooltipSize = tooltipRectTransform.rect.size;
            var pivot = tooltipRectTransform.pivot;

            var minX = canvasRect.xMin + tooltipSize.x * pivot.x;
            var maxX = canvasRect.xMax - tooltipSize.x * (1f - pivot.x);
            var minY = canvasRect.yMin + tooltipSize.y * pivot.y;
            var maxY = canvasRect.yMax - tooltipSize.y * (1f - pivot.y);

            if (minX > maxX)
            {
                localPosition.x = canvasRect.center.x;
            }
            else
            {
                localPosition.x = Mathf.Clamp(localPosition.x, minX, maxX);
            }

            if (minY > maxY)
            {
                localPosition.y = canvasRect.center.y;
            }
            else
            {
                localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);
            }

            return localPosition;
        }
    }
}
