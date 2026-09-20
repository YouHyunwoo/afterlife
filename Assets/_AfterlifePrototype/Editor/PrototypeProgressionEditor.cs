using System;
using System.IO;
using System.Linq;
using Afterlife.Modules.Progression;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Prototype.Editor
{
    public sealed class PrototypeProgressionEditor : EditorWindow
    {
        public const string CatalogPath = "Assets/_AfterlifePrototype/Resources/AfterlifePrototype/Progression.asset";
        private ProgressionCatalog _catalog;
        private int _treeIndex, _selected = -1;
        private Vector2 _pan, _inspectorScroll;
        private float _zoom = .85f;
        private bool _dragging, _panning, _pendingSave;
        private Vector2Int _originalGrid;
        private Vector2 _dragOffset;
        private double _saveAt;
        private string _status = "자동 저장 준비";
        private static readonly Vector2 Cell = new Vector2(120, 100);
        [MenuItem("Window/Afterlife/강화 에디터")]
        public static void Open() { GetWindow<PrototypeProgressionEditor>("강화 에디터"); }
        [InitializeOnLoadMethod]
        private static void ScheduleCatalog() { EditorApplication.delayCall += EnsureCatalog; }
        private static void EnsureCatalog()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += EnsureCatalog; return; }
            if (AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(CatalogPath) != null) return;
            if (File.Exists(CatalogPath)) { Debug.LogError("기존 Progression.asset을 읽을 수 없습니다. 덮어쓰지 않습니다."); return; }
            var catalog = CreateInstance<ProgressionCatalog>(); PrototypeProgressionDefaults.Populate(catalog);
            AssetDatabase.CreateAsset(catalog, CatalogPath); AssetDatabase.SaveAssetIfDirty(catalog);
        }
        private void OnEnable()
        {
            minSize = new Vector2(850, 520); _catalog = AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(CatalogPath);
            EditorApplication.update += UpdateSave; Undo.undoRedoPerformed += OnUndo;
        }
        private void OnDisable() { FinishDrag(); Save(); EditorApplication.update -= UpdateSave; Undo.undoRedoPerformed -= OnUndo; }
        private void OnLostFocus() { FinishDrag(); Save(); }
        private void OnUndo() { QueueSave(); Repaint(); }
        private void QueueSave() { if (_catalog == null) return; EditorUtility.SetDirty(_catalog); _pendingSave = true; _saveAt = EditorApplication.timeSinceStartup + .6; }
        private void UpdateSave() { if (_pendingSave && !_dragging && EditorApplication.timeSinceStartup >= _saveAt) Save(); }
        private void Save()
        {
            if (!_pendingSave || _catalog == null) return;
            try { AssetDatabase.SaveAssetIfDirty(_catalog); _pendingSave = false; _status = "자동 저장 " + DateTime.Now.ToString("HH:mm:ss"); }
            catch (Exception exception) { _status = "저장 실패: " + exception.Message; _saveAt = EditorApplication.timeSinceStartup + 3; }
        }
        private UpgradeTreeData Tree => _catalog.Trees[_treeIndex];
        private void OnGUI()
        {
            if (_catalog == null) _catalog = AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(CatalogPath);
            if (_catalog == null) { EditorGUILayout.HelpBox("기본 강화 데이터 생성 대기 중입니다.", MessageType.Info); return; }
            if (_catalog.Trees.Count == 0) { EditorGUILayout.HelpBox("데이터에 트리가 없습니다. Catalog Inspector에서 확인하세요.", MessageType.Error); return; }
            _treeIndex = Mathf.Clamp(_treeIndex, 0, _catalog.Trees.Count - 1);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                int next = GUILayout.Toolbar(_treeIndex, _catalog.Trees.Select(tree => tree.Title).ToArray(), EditorStyles.toolbarButton, GUILayout.Width(220));
                if (next != _treeIndex) { FinishDrag(); Save(); _treeIndex = next; _selected = -1; _pan = Vector2.zero; }
                if (GUILayout.Button("중앙 복귀", EditorStyles.toolbarButton, GUILayout.Width(85))) { _pan = Vector2.zero; _zoom = .85f; }
                if (GUILayout.Button("노드 추가", EditorStyles.toolbarButton, GUILayout.Width(85))) AddNode();
                if (GUILayout.Button("데이터 선택", EditorStyles.toolbarButton, GUILayout.Width(85))) Selection.activeObject = _catalog;
                GUILayout.FlexibleSpace(); GUILayout.Label(_status, EditorStyles.miniLabel); EditorGUILayout.EndHorizontal();
                var canvas = new Rect(0, 24, position.width - 340, position.height - 24);
                EditorGUI.DrawRect(canvas, new Color(.065f, .08f, .105f));
                DrawCanvas(canvas);
                GUILayout.BeginArea(new Rect(position.width - 330, 30, 320, position.height - 35));
                _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
                DrawInspector(); EditorGUILayout.EndScrollView(); GUILayout.EndArea();
            }
        }
        private Vector2 GridToScreen(Vector2 grid, Rect canvas) => canvas.center + _pan + Vector2.Scale(new Vector2(grid.x, -grid.y), Cell) * _zoom;
        private Vector2Int ScreenToGrid(Vector2 point, Rect canvas)
        {
            var delta = (point - canvas.center - _pan) / _zoom;
            return new Vector2Int(Mathf.RoundToInt(delta.x / Cell.x), Mathf.RoundToInt(-delta.y / Cell.y));
        }
        private void DrawCanvas(Rect canvas)
        {
            GUI.BeginClip(canvas);
            var local = new Rect(Vector2.zero, canvas.size);
            Handles.BeginGUI();
            var origin = local.center + _pan;
            Handles.color = new Color(.22f, .3f, .36f, .4f);
            for (float x = Mathf.Repeat(origin.x, Cell.x * _zoom); x < local.width; x += Cell.x * _zoom) Handles.DrawLine(new Vector3(x, 0), new Vector3(x, local.height));
            for (float y = Mathf.Repeat(origin.y, Cell.y * _zoom); y < local.height; y += Cell.y * _zoom) Handles.DrawLine(new Vector3(0, y), new Vector3(local.width, y));
            foreach (var node in Tree.Nodes)
                foreach (var parent in node.Parents.Count == 0 ? new[] { "" } : node.Parents.ToArray())
                {
                    var source = Tree.Nodes.Find(item => item.Id == parent); if (!string.IsNullOrEmpty(parent) && source == null) continue;
                    Handles.color = new Color(.3f, .7f, .65f, .8f);
                    Handles.DrawAAPolyLine(2, GridToScreen(source == null ? Vector2.zero : (Vector2)source.Grid, local), GridToScreen(node.Grid, local));
                }
            Handles.EndGUI();
            GUI.Box(new Rect(GridToScreen(Vector2.zero, local) - new Vector2(55, 24), new Vector2(110, 48)), Tree.Title + " 중심");
            for (int index = 0; index < Tree.Nodes.Count; index++)
            {
                var node = Tree.Nodes[index]; var size = new Vector2(88, 88) * _zoom;
                var box = new Rect(GridToScreen(node.Grid, local) - size / 2, size);
                EditorGUI.DrawRect(box, index == _selected ? new Color(.16f, .46f, .43f) : new Color(.12f, .22f, .29f));
                var style = new GUIStyle(EditorStyles.whiteLabel) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(9, Mathf.RoundToInt(13 * _zoom)), wordWrap = true };
                if (node.Icon != null)
                {
                    var iconRect = new Rect(box.x + 16 * _zoom, box.y + 5 * _zoom, 56 * _zoom, 56 * _zoom);
                    var spriteRect = node.Icon.rect; var texture = node.Icon.texture;
                    GUI.DrawTextureWithTexCoords(iconRect, texture, new Rect(spriteRect.x / texture.width, spriteRect.y / texture.height, spriteRect.width / texture.width, spriteRect.height / texture.height));
                    GUI.Label(new Rect(box.x, box.yMax - 24 * _zoom, box.width, 22 * _zoom), node.Id + " · " + node.Levels.Count + "단계", style);
                }
                else GUI.Label(box, node.Id + "\n" + node.Title, style);
            }
            HandleInput(local);
            GUI.EndClip();
        }
        private void HandleInput(Rect canvas)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var input = Event.current;
            if (input.type == EventType.MouseUp && (_dragging || _panning)) { FinishDrag(); _panning = false; input.Use(); return; }
            if (!canvas.Contains(input.mousePosition) && !_dragging && !_panning) return;
            if (input.type == EventType.ScrollWheel)
            {
                float next = Mathf.Clamp(_zoom * Mathf.Pow(1.1f, -input.delta.y), .35f, 1.6f);
                _pan = input.mousePosition - canvas.center - (input.mousePosition - canvas.center - _pan) * next / _zoom; _zoom = next; input.Use(); Repaint();
            }
            else if (input.type == EventType.MouseDown && input.button == 0)
            {
                _selected = Tree.Nodes.FindLastIndex(node => new Rect(GridToScreen(node.Grid, canvas) - new Vector2(44, 44) * _zoom, new Vector2(88, 88) * _zoom).Contains(input.mousePosition));
                if (_selected >= 0)
                {
                    Undo.RecordObject(_catalog, "강화 노드 격자 이동"); _originalGrid = Tree.Nodes[_selected].Grid;
                    _dragOffset = input.mousePosition - GridToScreen(_originalGrid, canvas); _dragging = true;
                }
                else _panning = true;
                GUI.FocusControl(null); input.Use(); Repaint();
            }
            else if (input.type == EventType.MouseDrag)
            {
                if (_dragging && _selected >= 0) { Tree.Nodes[_selected].Grid = ScreenToGrid(input.mousePosition - _dragOffset, canvas); QueueSave(); }
                else if (_panning) _pan += input.delta;
                input.Use(); Repaint();
            }
        }
        private void FinishDrag()
        {
            if (!_dragging || _catalog == null || _selected < 0 || _selected >= Tree.Nodes.Count) { _dragging = false; return; }
            var node = Tree.Nodes[_selected];
            if (node.Grid == Vector2Int.zero || Tree.Nodes.Where((_, index) => index != _selected).Any(item => item.Grid == node.Grid))
            { node.Grid = _originalGrid; _status = "겹친 격자: 원래 위치로 복원"; }
            _dragging = false; QueueSave();
        }
        private void DrawInspector()
        {
            EditorGUILayout.LabelField("노드 드래그 → 격자 스냅 → 자동 저장", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("빈 곳 드래그: 이동 / 휠: 확대·축소", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            if (_selected >= 0 && _selected < Tree.Nodes.Count)
            {
                var serialized = new SerializedObject(_catalog); serialized.Update();
                var node = serialized.FindProperty("Trees").GetArrayElementAtIndex(_treeIndex).FindPropertyRelative("Nodes").GetArrayElementAtIndex(_selected);
                EditorGUILayout.PropertyField(node, new GUIContent("선택한 강화"), true);
                if (serialized.ApplyModifiedProperties()) QueueSave();
                EditorGUILayout.HelpBox("Parents에는 같은 트리의 ID를 입력합니다. 부모 하나 이상 구매 시 표시, 모두 1단계 이상 구매 시 해금됩니다. Levels의 Value는 단계별 총효과입니다.", MessageType.Info);
                if (GUILayout.Button("선택 노드 삭제"))
                {
                    Undo.RecordObject(_catalog, "강화 노드 삭제"); string id = Tree.Nodes[_selected].Id;
                    Tree.Nodes.RemoveAt(_selected); foreach (var other in Tree.Nodes) other.Parents.RemoveAll(parent => parent == id);
                    _selected = -1; QueueSave();
                }
            }
            EditorGUILayout.Space();
            var skillObject = new SerializedObject(_catalog); skillObject.Update();
            EditorGUILayout.PropertyField(skillObject.FindProperty("Skills"), new GUIContent("스킬 시간 설정"), true);
            if (skillObject.ApplyModifiedProperties()) QueueSave();
            foreach (var error in PrototypeProgressionDefaults.Validate(_catalog)) EditorGUILayout.HelpBox(error, MessageType.Error);
            EditorGUILayout.HelpBox("오류가 있는 편집 내용도 초안으로 자동 저장합니다. 오류를 해결한 뒤 플레이하세요. 실행 중 데이터 편집은 지원하지 않습니다.", MessageType.None);
        }
        private void AddNode()
        {
            Undo.RecordObject(_catalog, "강화 노드 추가"); int count = 1; string id;
            do { id = Tree.Id + "." + count++; } while (_catalog.Nodes.Any(node => node.Id == id));
            var grid = new Vector2Int(1, 0); while (Tree.Nodes.Any(node => node.Grid == grid)) grid.x++;
            Tree.Nodes.Add(new UpgradeNodeData { Id = id, Title = "새 강화", Grid = grid, Levels = new System.Collections.Generic.List<UpgradeLevelData> { new UpgradeLevelData() } });
            _selected = Tree.Nodes.Count - 1; QueueSave();
        }
    }
}
