using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Dev.UpgradeTree.Data;
using Afterlife.Domain.Progression.Effect.Data;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Afterlife.Dev.UpgradeTree.Editor
{
    public class UpgradeTreeEditorWindow : EditorWindow
    {
        private UpgradeTreeData _treeData;
        private UpgradeTreeGraphView _graphView;
        private IMGUIContainer _toolbarContainer;
        private IMGUIContainer _inspectorContainer;
        private UpgradeTreeNodeData _selectedNode;
        private Vector2 _inspectorScroll;

        [MenuItem("Tools/Afterlife/Dev Upgrade Tree Editor")]
        public static void Open()
        {
            GetWindow<UpgradeTreeEditorWindow>("Dev Upgrade Tree");
        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            _toolbarContainer = new IMGUIContainer(DrawToolbar);
            rootVisualElement.Add(_toolbarContainer);

            var content = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1f
                }
            };
            rootVisualElement.Add(content);

            _graphView = new UpgradeTreeGraphView();
            _graphView.style.flexGrow = 1f;
            _graphView.OnNodeSelected = node =>
            {
                _selectedNode = node;
                _inspectorContainer.MarkDirtyRepaint();
            };
            _graphView.OnGraphChanged = MarkTreeDirty;
            content.Add(_graphView);

            _inspectorContainer = new IMGUIContainer(DrawInspector)
            {
                style =
                {
                    width = 340f,
                    flexShrink = 0f
                }
            };
            content.Add(_inspectorContainer);
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is UpgradeTreeData selectedTreeData)
            {
                _treeData = selectedTreeData;
                RebuildGraph(true);
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            _treeData = (UpgradeTreeData)EditorGUILayout.ObjectField(_treeData, typeof(UpgradeTreeData), false, GUILayout.Width(260f));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedNode = null;
                RebuildGraph(true);
            }

            using (new EditorGUI.DisabledScope(_treeData == null))
            {
                if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    AddNode();
                }

                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    ShowValidation();
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    EditorUtility.SetDirty(_treeData);
                    AssetDatabase.SaveAssets();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInspector()
        {
            if (_treeData == null)
            {
                EditorGUILayout.HelpBox("Select an UpgradeTreeData asset.", MessageType.Info);
                return;
            }

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            DrawTreeSettings();
            EditorGUILayout.Space(8f);
            DrawNodeInspector();
            EditorGUILayout.EndScrollView();
        }

        private void DrawTreeSettings()
        {
            EditorGUILayout.LabelField("Runtime Tree", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var cellSize = EditorGUILayout.Vector2Field("Cell Size", _treeData.CellSize);
            var nodeSize = EditorGUILayout.Vector2Field("Node Size", _treeData.NodeSize);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_treeData, "Edit Upgrade Tree Settings");
                _treeData.CellSize = cellSize;
                _treeData.NodeSize = nodeSize;
                MarkTreeDirty();
                RebuildGraph();
            }
        }

        private void DrawNodeInspector()
        {
            EditorGUILayout.LabelField("Selected Node", EditorStyles.boldLabel);
            if (_selectedNode == null)
            {
                EditorGUILayout.HelpBox("Select a node in the graph.", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            var id = EditorGUILayout.TextField("Id", _selectedNode.Id);
            var name = EditorGUILayout.TextField("Name", _selectedNode.Name);
            var icon = (Sprite)EditorGUILayout.ObjectField("Icon", _selectedNode.Icon, typeof(Sprite), false);
            var gridPosition = EditorGUILayout.Vector2IntField("Grid Position", _selectedNode.GridPosition);

            EditorGUILayout.Space(6f);
            DrawStringArray("Child Node Ids", ref _selectedNode.ChildNodeIds);
            EditorGUILayout.Space(6f);
            DrawLevels();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_treeData, "Edit Upgrade Tree Node");
                _selectedNode.Id = id;
                _selectedNode.Name = name;
                _selectedNode.Icon = icon;
                _selectedNode.GridPosition = gridPosition;
                MarkTreeDirty();
                RebuildGraph();
            }

            if (GUILayout.Button("Delete Node"))
            {
                DeleteNode(_selectedNode);
            }
        }

        private void DrawStringArray(string label, ref string[] values)
        {
            values ??= Array.Empty<string>();
            EditorGUILayout.LabelField(label);
            var removeIndex = -1;
            for (var i = 0; i < values.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                values[i] = EditorGUILayout.TextField(values[i]);
                if (GUILayout.Button("-", GUILayout.Width(24f))) { removeIndex = i; }
                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                values = values.Where((_, index) => index != removeIndex).ToArray();
            }

            if (GUILayout.Button($"Add {label}"))
            {
                Array.Resize(ref values, values.Length + 1);
                values[^1] = string.Empty;
            }
        }

        private void DrawLevels()
        {
            _selectedNode.Levels ??= Array.Empty<UpgradeTreeLevelData>();
            EditorGUILayout.LabelField("Levels");

            var removeIndex = -1;
            for (var i = 0; i < _selectedNode.Levels.Length; i++)
            {
                var level = _selectedNode.Levels[i] ??= new UpgradeTreeLevelData();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Level {i + 1}", EditorStyles.boldLabel);
                level.Cost = EditorGUILayout.IntField("Cost", level.Cost);
                level.Description = EditorGUILayout.TextField("Description", level.Description);
                DrawParameters(level);
                if (GUILayout.Button("Remove Level")) { removeIndex = i; }
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                _selectedNode.Levels = _selectedNode.Levels.Where((_, index) => index != removeIndex).ToArray();
            }

            if (GUILayout.Button("Add Level"))
            {
                Array.Resize(ref _selectedNode.Levels, _selectedNode.Levels.Length + 1);
                _selectedNode.Levels[^1] = new UpgradeTreeLevelData();
            }
        }

        private void DrawParameters(UpgradeTreeLevelData level)
        {
            level.Parameters ??= Array.Empty<PlayerEffectParameter>();
            EditorGUILayout.LabelField("Parameters");

            var removeIndex = -1;
            for (var i = 0; i < level.Parameters.Length; i++)
            {
                var parameter = level.Parameters[i] ??= new PlayerEffectParameter();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                parameter.Name = EditorGUILayout.TextField("Name", parameter.Name);
                DrawStringArray("Values", ref parameter.Values);
                if (GUILayout.Button("Remove Parameter")) { removeIndex = i; }
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                level.Parameters = level.Parameters.Where((_, index) => index != removeIndex).ToArray();
            }

            if (GUILayout.Button("Add Parameter"))
            {
                Array.Resize(ref level.Parameters, level.Parameters.Length + 1);
                level.Parameters[^1] = new PlayerEffectParameter();
            }
        }

        private void AddNode()
        {
            Undo.RecordObject(_treeData, "Add Upgrade Tree Node");
            var nodes = _treeData.Nodes?.ToList() ?? new List<UpgradeTreeNodeData>();
            var node = new UpgradeTreeNodeData
            {
                Id = CreateUniqueNodeId(nodes),
                Name = "New Node",
                GridPosition = Vector2Int.zero,
                Levels = new[] { new UpgradeTreeLevelData() }
            };
            nodes.Add(node);
            _treeData.Nodes = nodes.ToArray();
            _selectedNode = node;
            MarkTreeDirty();
            RebuildGraph();
        }

        private string CreateUniqueNodeId(List<UpgradeTreeNodeData> nodes)
        {
            var index = nodes.Count + 1;
            var id = $"new-node-{index}";
            while (nodes.Any(node => node != null && node.Id == id))
            {
                index++;
                id = $"new-node-{index}";
            }

            return id;
        }

        private void DeleteNode(UpgradeTreeNodeData node)
        {
            if (node == null) { return; }

            Undo.RecordObject(_treeData, "Delete Upgrade Tree Node");
            _treeData.Nodes = _treeData.Nodes.Where(candidate => candidate != node).ToArray();
            foreach (var candidate in _treeData.Nodes)
            {
                if (candidate == null) { continue; }
                candidate.ChildNodeIds = RemoveId(candidate.ChildNodeIds, node.Id);
            }

            _selectedNode = null;
            MarkTreeDirty();
            RebuildGraph();
        }

        private string[] RemoveId(string[] values, string id)
        {
            return values == null ? Array.Empty<string>() : values.Where(value => value != id).ToArray();
        }

        private void RebuildGraph(bool centerOnOrigin = false)
        {
            _graphView?.Populate(_treeData, centerOnOrigin);
            _inspectorContainer?.MarkDirtyRepaint();
        }

        private void ShowValidation()
        {
            var errors = UpgradeTreeValidator.Validate(_treeData);
            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog("Upgrade Tree Validation", "No validation errors.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("Upgrade Tree Validation", string.Join("\n", errors), "OK");
        }

        private void MarkTreeDirty()
        {
            if (_treeData == null) { return; }

            EditorUtility.SetDirty(_treeData);
            _toolbarContainer?.MarkDirtyRepaint();
            _inspectorContainer?.MarkDirtyRepaint();
        }
    }

    internal class UpgradeTreeGraphView : GraphView
    {
        private const string ChildPortName = "Child";
        private static readonly Vector2 EditorGridCellSize = new(260f, 180f);
        private static readonly Vector2 EditorNodeSize = new(220f, 140f);
        private UpgradeTreeData _treeData;
        private readonly Dictionary<string, UpgradeTreeNodeElement> _nodeElements = new();
        private bool _isPopulating;

        public Action<UpgradeTreeNodeData> OnNodeSelected;
        public Action OnGraphChanged;

        public UpgradeTreeGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            var gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            graphViewChanged = HandleGraphViewChanged;
        }

        public void Populate(UpgradeTreeData treeData, bool centerOnOrigin)
        {
            _treeData = treeData;
            _isPopulating = true;
            DeleteElements(graphElements.ToList());
            _nodeElements.Clear();
            _isPopulating = false;

            if (_treeData?.Nodes == null) { return; }

            foreach (var nodeData in _treeData.Nodes)
            {
                if (nodeData == null || string.IsNullOrEmpty(nodeData.Id)) { continue; }

                var nodeElement = new UpgradeTreeNodeElement(nodeData, EditorGridCellSize, EditorNodeSize);
                nodeElement.OnNodeSelected = OnNodeSelected;
                AddElement(nodeElement);
                _nodeElements[nodeData.Id] = nodeElement;
            }

            foreach (var nodeData in _treeData.Nodes)
            {
                AddConnectionEdges(nodeData);
            }

            if (centerOnOrigin)
            {
                CenterGridOrigin();
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .Where(port => port != startPort && port.node != startPort.node && port.direction != startPort.direction)
                .ToList();
        }

        private GraphViewChange HandleGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_isPopulating) { return graphViewChange; }
            if (_treeData == null) { return graphViewChange; }

            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var element in graphViewChange.elementsToRemove)
                {
                    if (element is Edge edge)
                    {
                        RemoveEdgeData(edge);
                    }
                    else if (element is UpgradeTreeNodeElement nodeElement)
                    {
                        RemoveNodeData(nodeElement.NodeData);
                    }
                }
            }

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    AddEdgeData(edge);
                    ColorEdge(edge);
                }
            }

            if (graphViewChange.movedElements != null)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                    if (element is UpgradeTreeNodeElement nodeElement)
                    {
                        nodeElement.SnapToGrid();
                    }
                }
            }

            OnGraphChanged?.Invoke();
            return graphViewChange;
        }

        private void CenterGridOrigin()
        {
            schedule.Execute(() =>
            {
                if (layout.width <= 0f || layout.height <= 0f) { return; }

                var graphCenter = Vector2.zero;
                foreach (var nodeElement in _nodeElements.Values)
                {
                    if (nodeElement.NodeData.GridPosition != Vector2Int.zero) { continue; }

                    graphCenter = nodeElement.GetPosition().center;
                    break;
                }

                var viewCenter = new Vector3(layout.width * 0.5f, layout.height * 0.5f, 0f);
                UpdateViewTransform(viewCenter - new Vector3(graphCenter.x, graphCenter.y, 0f), Vector3.one);
            }).ExecuteLater(0);
        }

        private void AddConnectionEdges(UpgradeTreeNodeData nodeData)
        {
            if (nodeData == null) { return; }
            if (!_nodeElements.TryGetValue(nodeData.Id, out var sourceElement)) { return; }

            var targetIds = nodeData.ChildNodeIds;
            if (targetIds == null) { return; }

            foreach (var targetId in targetIds)
            {
                if (!_nodeElements.TryGetValue(targetId, out var targetElement)) { continue; }

                var edge = sourceElement.ChildOutputPort.ConnectTo(targetElement.InputPort);
                ColorEdge(edge);
                AddElement(edge);
            }
        }

        private void AddEdgeData(Edge edge)
        {
            if (edge?.output?.node is not UpgradeTreeNodeElement sourceElement ||
                edge.input?.node is not UpgradeTreeNodeElement targetElement)
            {
                return;
            }

            if (edge.output.portName == ChildPortName)
            {
                sourceElement.NodeData.ChildNodeIds = AddUniqueId(sourceElement.NodeData.ChildNodeIds, targetElement.NodeData.Id);
            }
        }

        private void RemoveEdgeData(Edge edge)
        {
            if (edge?.output?.node is not UpgradeTreeNodeElement sourceElement ||
                edge.input?.node is not UpgradeTreeNodeElement targetElement)
            {
                return;
            }

            if (edge.output.portName == ChildPortName)
            {
                sourceElement.NodeData.ChildNodeIds = RemoveId(sourceElement.NodeData.ChildNodeIds, targetElement.NodeData.Id);
            }
        }

        private void RemoveNodeData(UpgradeTreeNodeData nodeData)
        {
            if (_treeData?.Nodes == null || nodeData == null) { return; }

            _treeData.Nodes = _treeData.Nodes.Where(node => node != nodeData).ToArray();
            foreach (var node in _treeData.Nodes)
            {
                if (node == null) { continue; }
                node.ChildNodeIds = RemoveId(node.ChildNodeIds, nodeData.Id);
            }
        }

        private string[] AddUniqueId(string[] values, string id)
        {
            values ??= Array.Empty<string>();
            return values.Contains(id) ? values : values.Concat(new[] { id }).ToArray();
        }

        private string[] RemoveId(string[] values, string id)
        {
            return values == null ? Array.Empty<string>() : values.Where(value => value != id).ToArray();
        }

        private void ColorEdge(Edge edge)
        {
            if (edge?.output == null) { return; }

            var color = new Color(0.42f, 0.68f, 1f);
            edge.edgeControl.inputColor = color;
            edge.edgeControl.outputColor = color;
        }
    }

    internal class UpgradeTreeNodeElement : Node
    {
        private readonly Vector2 _cellSize;

        public UpgradeTreeNodeData NodeData { get; }
        public Port InputPort { get; }
        public Port ChildOutputPort { get; }
        public Action<UpgradeTreeNodeData> OnNodeSelected;

        public UpgradeTreeNodeElement(UpgradeTreeNodeData nodeData, Vector2 cellSize, Vector2 nodeSize)
        {
            NodeData = nodeData;
            _cellSize = cellSize;
            title = string.IsNullOrEmpty(nodeData.Name) ? nodeData.Id : nodeData.Name;

            InputPort = CreatePort("In", Direction.Input);
            inputContainer.Add(InputPort);

            ChildOutputPort = CreatePort("Child", Direction.Output);
            outputContainer.Add(ChildOutputPort);

            RefreshExpandedState();
            RefreshPorts();
            SetPosition(new Rect(GridToPosition(nodeData.GridPosition), nodeSize));
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(NodeData);
        }

        public void SnapToGrid()
        {
            var position = GetPosition();
            var gridPosition = new Vector2Int(
                Mathf.RoundToInt(position.x / Mathf.Max(1f, _cellSize.x)),
                Mathf.RoundToInt(position.y / Mathf.Max(1f, _cellSize.y)));

            NodeData.GridPosition = gridPosition;
            SetPosition(new Rect(GridToPosition(gridPosition), position.size));
        }

        private Vector2 GridToPosition(Vector2Int gridPosition)
        {
            return new Vector2(gridPosition.x * _cellSize.x, gridPosition.y * _cellSize.y);
        }

        private Port CreatePort(string portName, Direction direction)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, direction, Port.Capacity.Multi, typeof(bool));
            port.portName = portName;
            return port;
        }
    }
}
