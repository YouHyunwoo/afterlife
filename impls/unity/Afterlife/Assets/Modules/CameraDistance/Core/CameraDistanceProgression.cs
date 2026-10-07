using System;

namespace Afterlife.Modules.CameraDistance
{
    /// <summary>초기 마을 영역에서 늘어난 타일 수를 목표 카메라 거리로 바꾼다.</summary>
    public sealed class CameraDistanceProgression
    {
        public const float DefaultStartDistance = 16f;
        public const float DefaultMaximumDistance = 22.7f;
        public const int DefaultExpansionTilesToMaximum = 120;
        public const float DefaultFollowSpeed = 3f;

        private float _distance;
        private float _startDistance;
        private int _initialTerritoryTiles;
        private bool _initialized;

        public float Distance => _distance;

        public void Reset(float startDistance, float maximumDistance, int territoryTiles)
        {
            _startDistance = ValidDistance(startDistance, DefaultStartDistance);
            _distance = Math.Min(_startDistance, ValidDistance(maximumDistance, DefaultMaximumDistance));
            _initialTerritoryTiles = Math.Max(0, territoryTiles);
            _initialized = true;
        }

        public void FollowTerritory(int territoryTiles, float startDistance, float maximumDistance,
            int expansionTilesToMaximum, float followSpeed, float seconds)
        {
            startDistance = ValidDistance(startDistance, DefaultStartDistance);
            maximumDistance = ValidDistance(maximumDistance, DefaultMaximumDistance);
            if (!_initialized || startDistance != _startDistance)
                Reset(startDistance, maximumDistance, territoryTiles);

            // 최대 거리를 실행 중 낮춰도 그 상한은 즉시 지킨다.
            _distance = Math.Min(_distance, maximumDistance);
            int addedTiles = Math.Max(0, territoryTiles - _initialTerritoryTiles);
            float expansion = Math.Min(1f, (float)addedTiles / Math.Max(1, expansionTilesToMaximum));
            float target = Math.Min(startDistance, maximumDistance)
                + Math.Max(0f, maximumDistance - startDistance) * expansion;
            if (!IsFinite(seconds) || seconds <= 0 || !IsFinite(followSpeed) || followSpeed <= 0) return;

            float step = followSpeed * seconds;
            if (!IsFinite(step)) { _distance = target; return; }
            _distance += Math.Max(-step, Math.Min(step, target - _distance));
        }

        private static float ValidDistance(float value, float fallback)
        {
            return IsFinite(value) ? Math.Max(1f, value) : fallback;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
