using System;
using System.Collections.Generic;
using Afterlife.Dev.World;
using Afterlife.Model;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class FogSystem : Moonstone.Ore.Local.System
    {
        [Header("Fog")]
        [SerializeField] private Sprite _fogSprite;
        [SerializeField] private Transform _fogContainer;

        private Vector2Int _size;
        private FogLayer _fogLayer;
        private SpriteRenderer[,] _renderers;

        private readonly HashSet<Vector2Int> _previousLitCells = new();
        private readonly List<(Transform source, Model.Light light)> _lights = new();
        private bool _isDirty;

        public FogLayer FogLayer => _fogLayer;
        public event Action<FogLayer> OnFogUpdated;

        // ── Public API ────────────────────────────────────────────────────────

        public void Build(Vector2Int size)
        {
            _size = size;
            _fogLayer = new FogLayer(size);
            _renderers = new SpriteRenderer[size.x, size.y];

            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    var go = new GameObject($"FogTile ({x},{y})");
                    go.transform.SetParent(_fogContainer, worldPositionStays: false);
                    go.transform.localPosition = new Vector3(x + 0.5f, y + 0.5f, 0f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = _fogSprite;
                    sr.color = new Color(0f, 0f, 0f, 1f);
                    sr.sortingLayerID = SortingLayer.NameToID("Fog");
                    _renderers[x, y] = sr;
                }
            }

            _isDirty = true;
        }

        public void AddLight(Transform source, Model.Light light)
        {
            _lights.Add((source, light));
            _isDirty = true;
        }

        public void RemoveLight(Model.Light light)
        {
            _lights.RemoveAll(x => x.light == light);
            _isDirty = true;
        }

        public void Invalidate() => _isDirty = true;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void OnSetUp() => _isDirty = true;

        protected override void OnTearDown()
        {
            if (_renderers == null) return;

            for (var x = 0; x < _size.x; x++)
                for (var y = 0; y < _size.y; y++)
                    if (_renderers[x, y] != null)
                        Destroy(_renderers[x, y].gameObject);

            _renderers = null;
            _fogLayer = null;
            _previousLitCells.Clear();
            _lights.Clear();
            _isDirty = false;
        }

        // ── Update ────────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (_fogLayer == null) return;

            // 위치 자동 동기화: 매 프레임 transform 위치를 Light.Location에 반영
            // 위치가 바뀌면 dirty → 다음 연산에서 안개 갱신
            foreach (var (source, light) in _lights)
            {
                if (source == null) continue;
                var newPos = new Vector2Int(Mathf.FloorToInt(source.position.x),
                                           Mathf.FloorToInt(source.position.y));
                if (newPos != light.Location) { light.Location = newPos; _isDirty = true; }
            }

            if (!_isDirty) return;

            var cells = _fogLayer.Cells;
            var blackFull = new Color(0f, 0f, 0f, 1f);

            // Opt #1: 이전 프레임에 밝아진 셀만 리셋 (전체 그리드 리셋 X)
            foreach (var cell in _previousLitCells)
            {
                cells[cell.x, cell.y] = 1f;
                _renderers[cell.x, cell.y].color = blackFull;
            }
            _previousLitCells.Clear();

            // Opt #4: HashSet으로 중복 제거
            var currentLit = new HashSet<Vector2Int>();

            for (var i = 0; i < _lights.Count; i++)
            {
                var (_, light) = _lights[i];
                if (!light.IsActive) continue;

                var loc = light.Location;
                var range = light.Range;
                var intensity = light.Intensity;

                // Opt #2: 루프 전 경계 클램프 (내부 루프에서 경계 검사 제거)
                var xMin = Mathf.Max(0, Mathf.FloorToInt(loc.x - range));
                var xMax = Mathf.Min(_size.x - 1, Mathf.CeilToInt(loc.x + range));
                var yMin = Mathf.Max(0, Mathf.FloorToInt(loc.y - range));
                var yMax = Mathf.Min(_size.y - 1, Mathf.CeilToInt(loc.y + range));

                var rangeSq = range * range;
                var thresholdSq = (intensity - 1f) * (intensity - 1f);
                var denom = range - intensity + 1f;

                for (var x = xMin; x <= xMax; x++)
                {
                    var dx = x - loc.x;
                    var dxSq = dx * dx;

                    // Opt #3a: 열 단위 조기 탈락
                    if (dxSq > rangeSq) continue;

                    for (var y = yMin; y <= yMax; y++)
                    {
                        var dy = y - loc.y;
                        var distSq = dxSq + dy * dy;

                        // Opt #3b: 제곱 거리 비교로 원 밖 셀 탈락 (sqrt 최소화)
                        if (distSq > rangeSq) continue;

                        float brightness;
                        if (distSq < thresholdSq)
                        {
                            brightness = 1f;
                        }
                        else
                        {
                            // sqrt는 실제 밝기 계산이 필요한 셀에서만 호출
                            var dist = Mathf.Sqrt(distSq);
                            brightness = 1f - (dist - intensity + 1f) / denom;
                        }

                        var fog = 1f - brightness;
                        var pos = new Vector2Int(x, y);

                        // Opt #4: 첫 조명은 직접 쓰기, 겹치는 조명은 min(가장 밝음)
                        if (currentLit.Add(pos))
                            cells[x, y] = fog;
                        else
                            cells[x, y] = Mathf.Min(cells[x, y], fog);

                        // FogLayer + SpriteRenderer 단일 패스 갱신
                        _renderers[x, y].color = new Color(0f, 0f, 0f, cells[x, y]);
                    }
                }
            }

            _previousLitCells.UnionWith(currentLit);

            _isDirty = false;
            OnFogUpdated?.Invoke(_fogLayer);
        }
    }
}
