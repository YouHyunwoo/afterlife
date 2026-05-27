using System;
using System.Collections.Generic;
using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class FogSystem : ISystem<ComponentManager>
    {
        private readonly HashSet<Vector2Int> _previousLitCells = new();
        private bool _isDirty = true;

        public Func<Vector2Int, float> GetFog;
        public Action<Vector2Int, float> SetFog;
        public Action<Vector2Int, Color> SetRendererAlpha;

        public void Update(float deltaTime, ComponentManager cm)
        {
            var isDirty = CheckDirty(cm);
            if (!isDirty && !_isDirty) return;

            ResetPreviousLitCells();
            UpdateFog(cm);

            _isDirty = false;
        }

        private bool CheckDirty(ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasLight[i]) continue;
                if (!cm.HasNavMeshAgent[i]) continue;

                var light = cm.Lights[i];
                var agent = cm.NavMeshAgents[i];

                if (light.Intensity > 0 && light.Range > 0 && agent.hasPath) return true;
            }

            return false;
        }

        private void ResetPreviousLitCells()
        {
            var blackFull = new Color(0f, 0f, 0f, 1f);

            // Opt #1: 이전 프레임에 밝아진 셀만 리셋 (전체 그리드 리셋 X)
            foreach (var cell in _previousLitCells)
            {
                SetFog(cell, 1f);
                SetRendererAlpha(cell, blackFull);
            }
            _previousLitCells.Clear();
        }

        private void UpdateFog(ComponentManager cm)
        {
            var worldMapSize = cm.Game.World.Size;
            var currentLit = new HashSet<Vector2Int>();

            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasEntity[i]) continue;
                if (!cm.HasLight[i]) continue;

                var entity = cm.Entities[i];
                var light = cm.Lights[i];

                if (!light.IsActive) continue;

                var lightPosition = (Vector2)entity.transform.position;
                var lightRange = light.Range;
                var lightIntensity = light.Intensity;

                // Opt #2: 루프 전 경계 클램프 (내부 루프에서 경계 검사 제거)
                var xMin = Mathf.Max(0, Mathf.FloorToInt(lightPosition.x - lightRange));
                var xMax = Mathf.Min(worldMapSize.x - 1, Mathf.CeilToInt(lightPosition.x + lightRange));
                var yMin = Mathf.Max(0, Mathf.FloorToInt(lightPosition.y - lightRange));
                var yMax = Mathf.Min(worldMapSize.y - 1, Mathf.CeilToInt(lightPosition.y + lightRange));

                var rangeSq = lightRange * lightRange;
                var thresholdSq = (lightIntensity - 1f) * (lightIntensity - 1f);
                var denom = lightRange - lightIntensity + 1f;

                for (var x = xMin; x <= xMax; x++)
                {
                    var dx = x + 0.5f - lightPosition.x;
                    var dxSq = dx * dx;

                    // Opt #3a: 열 단위 조기 탈락
                    if (dxSq > rangeSq) continue;

                    for (var y = yMin; y <= yMax; y++)
                    {
                        var dy = y + 0.5f - lightPosition.y;
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
                            brightness = 1f - (dist - lightIntensity + 1f) / denom;
                        }

                        var fog = 1f - brightness;
                        var pos = new Vector2Int(x, y);

                        // Opt #4: 첫 조명은 직접 쓰기, 겹치는 조명은 min(가장 밝음)
                        if (currentLit.Add(pos))
                            SetFog(pos, fog);
                        else
                            SetFog(pos, Mathf.Min(GetFog(pos), fog));

                        // FogLayer + SpriteRenderer 단일 패스 갱신
                        SetRendererAlpha(pos, new Color(0f, 0f, 0f, GetFog(pos)));
                    }
                }
            }

            _previousLitCells.UnionWith(currentLit);
        }

        public void Invalidate() => _isDirty = true;
    }
}
