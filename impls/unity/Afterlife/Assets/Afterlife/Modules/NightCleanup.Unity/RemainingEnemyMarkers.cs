using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Modules.NightCleanup.Unity
{
    public readonly struct EnemyMarkerTarget
    {
        public readonly int Id;
        public readonly Vector3 Position;
        public EnemyMarkerTarget(int id, Vector3 position) { Id = id; Position = position; }
    }

    // Add one component, then pass a camera and world positions from a game adapter.
    public sealed class RemainingEnemyMarkers : MonoBehaviour
    {
        private sealed class Marker
        {
            public LineRenderer Direction, Beacon;
            public GameObject Root;
        }
        private readonly Dictionary<int, Marker> _markers = new Dictionary<int, Marker>();
        private readonly HashSet<int> _live = new HashSet<int>();
        private readonly List<int> _removed = new List<int>();
        private Material _material;
        public void Present(Camera camera, IReadOnlyList<EnemyMarkerTarget> targets, bool direction, bool beacon)
        {
            if (!isActiveAndEnabled || camera == null || !camera.enabled || !direction) { Clear(); return; }
            if (_material == null)
            {
                var shader = Resources.Load<Shader>("NightCleanupMarker");
                if (shader == null) { Debug.LogError("Night cleanup marker shader is missing.", this); return; }
                _material = new Material(shader);
                _material.SetColor("_Color", new Color(1, .74f, .22f, .9f));
            }
            _live.Clear();
            foreach (var target in targets)
            {
                _live.Add(target.Id);
                if (!_markers.TryGetValue(target.Id, out var marker))
                {
                    marker = new Marker { Root = new GameObject("RemainingEnemyMarker") };
                    marker.Root.transform.SetParent(transform, false);
                    marker.Direction = CreateLine(marker.Root.transform, "Direction", 3, .004f);
                    marker.Beacon = CreateLine(marker.Root.transform, "Beacon", 2, .16f);
                    _markers.Add(target.Id, marker);
                }
                Vector3 viewport = camera.WorldToViewportPoint(target.Position + Vector3.up);
                Vector2 offset = new Vector2(viewport.x - .5f, viewport.y - .5f);
                if (viewport.z < 0) offset = -offset;
                if (offset.sqrMagnitude < .0001f) offset = Vector2.up * .001f;
                bool offscreen = viewport.z <= 0 || Mathf.Abs(offset.x) > .43f || Mathf.Abs(offset.y) > .4f;
                if (offscreen) offset *= Mathf.Min(.43f / Mathf.Max(.0001f, Mathf.Abs(offset.x)), .4f / Mathf.Max(.0001f, Mathf.Abs(offset.y)));
                float depth = camera.nearClipPlane + .5f;
                Vector3 tip = camera.ViewportToWorldPoint(new Vector3(.5f + offset.x, .5f + offset.y, depth));
                Vector2 pointing = offscreen ? new Vector2(offset.x * camera.aspect, offset.y).normalized : Vector2.down;
                Vector3 along = camera.transform.right * pointing.x + camera.transform.up * pointing.y;
                Vector3 side = camera.transform.right * -pointing.y + camera.transform.up * pointing.x;
                float size = camera.orthographic ? camera.orthographicSize * .025f : depth * .025f;
                marker.Direction.startWidth = marker.Direction.endWidth = size * .22f;
                marker.Direction.SetPosition(0, tip - along * size + side * size * .55f);
                marker.Direction.SetPosition(1, tip);
                marker.Direction.SetPosition(2, tip - along * size - side * size * .55f);
                marker.Beacon.enabled = beacon && viewport.z > 0;
                marker.Beacon.SetPosition(0, target.Position + Vector3.up * .2f);
                marker.Beacon.SetPosition(1, target.Position + Vector3.up * 8);
            }
            _removed.Clear();
            foreach (var pair in _markers) if (!_live.Contains(pair.Key)) _removed.Add(pair.Key);
            foreach (int id in _removed) { Destroy(_markers[id].Root); _markers.Remove(id); }
        }
        private LineRenderer CreateLine(Transform parent, string name, int count, float width)
        {
            var child = new GameObject(name); child.transform.SetParent(parent, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = _material; line.useWorldSpace = true; line.positionCount = count;
            line.startWidth = line.endWidth = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }
        public void Clear()
        {
            foreach (var marker in _markers.Values) { marker.Root.SetActive(false); Destroy(marker.Root); }
            _markers.Clear();
        }
        private void OnDisable() => Clear();
        private void OnDestroy() { Clear(); if (_material != null) Destroy(_material); }
    }
}
