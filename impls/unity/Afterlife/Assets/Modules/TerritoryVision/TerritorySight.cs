using System;
using System.Collections.Generic;

namespace Afterlife.TerritoryVision
{
    /// <summary>단위 타일 사각형 외곽에서 둥글게 확장한 시야. 조명도 같은 범위를 사용한다.</summary>
    public sealed class TerritorySight
    {
        public const float Radius = 2f;
        private readonly HashSet<(int x, int z)> _tiles = new HashSet<(int, int)>();
        private int _minX, _maxX, _minZ, _maxZ;
        public void Clear() => _tiles.Clear();
        public void AddTile(int x, int z)
        {
            if (_tiles.Count == 0) { _minX = _maxX = x; _minZ = _maxZ = z; }
            _tiles.Add((x, z));
            _minX = Math.Min(_minX, x); _maxX = Math.Max(_maxX, x);
            _minZ = Math.Min(_minZ, z); _maxZ = Math.Max(_maxZ, z);
        }
        public bool Contains(float x, float z) => DistanceSquared(x, z) <= Radius * Radius;

        private float DistanceSquared(float x, float z)
        {
            if (_tiles.Count == 0 || x < _minX - Radius - .5f || x > _maxX + Radius + .5f
                || z < _minZ - Radius - .5f || z > _maxZ + Radius + .5f) return float.PositiveInfinity;
            float nearest = float.PositiveInfinity;
            int minX = (int)Math.Ceiling(x - Radius - .5f), maxX = (int)Math.Floor(x + Radius + .5f);
            int minZ = (int)Math.Ceiling(z - Radius - .5f), maxZ = (int)Math.Floor(z + Radius + .5f);
            for (int tz = minZ; tz <= maxZ; tz++)
            for (int tx = minX; tx <= maxX; tx++)
            {
                if (!_tiles.Contains((tx, tz))) continue;
                float dx = Math.Max(0, Math.Abs(x - tx) - .5f);
                float dz = Math.Max(0, Math.Abs(z - tz) - .5f);
                nearest = Math.Min(nearest, dx * dx + dz * dz);
                if (nearest == 0) return 0;
            }
            return nearest;
        }
    }
}
