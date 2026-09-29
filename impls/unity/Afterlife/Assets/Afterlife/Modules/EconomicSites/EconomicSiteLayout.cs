using System;
using System.Collections.Generic;

namespace Afterlife.Modules.EconomicSites
{
    public enum EconomicSiteKind : byte { None, Fertile, Leyline }

    public readonly struct EconomicSite
    {
        public readonly EconomicSiteKind Kind;
        public readonly int X;
        public readonly int Z;
        public readonly int Size;

        public EconomicSite(EconomicSiteKind kind, int x, int z, int size)
        {
            Kind = kind;
            X = x;
            Z = z;
            Size = size;
        }

        public bool Contains(int x, int z) => x >= X && x < X + Size && z >= Z && z < Z + Size;
    }

    public readonly struct EconomicSiteSettings
    {
        public readonly int FertileCount;
        public readonly int LeylineCount;
        public readonly int FertileSize;
        public readonly int LeylineSize;
        public readonly int MinimumDistance;

        public EconomicSiteSettings(int fertileCount, int leylineCount, int fertileSize, int leylineSize, int minimumDistance)
        {
            FertileCount = Math.Max(0, fertileCount);
            LeylineCount = Math.Max(0, leylineCount);
            FertileSize = Math.Max(1, fertileSize);
            LeylineSize = Math.Max(1, leylineSize);
            MinimumDistance = Math.Max(0, minimumDistance);
        }
    }

    // 지형 셀과 시작 지점만 입력받는 판 단위 순수 데이터. 지도 시드가 같으면 결과도 같다.
    public sealed class EconomicSiteLayout
    {
        private readonly List<EconomicSite> _sites = new List<EconomicSite>();
        private readonly EconomicSiteKind[,] _cells;
        private readonly int _radius;

        public IReadOnlyList<EconomicSite> Sites => _sites;

        private EconomicSiteLayout(int radius)
        {
            _radius = radius;
            _cells = new EconomicSiteKind[radius * 2 + 1, radius * 2 + 1];
        }

        public EconomicSiteKind At(int x, int z)
        {
            if (x < -_radius || x > _radius || z < -_radius || z > _radius) return EconomicSiteKind.None;
            return _cells[x + _radius, z + _radius];
        }

        public bool Covers(EconomicSiteKind kind, int x, int z, int width, int depth)
        {
            if (kind == EconomicSiteKind.None || width < 1 || depth < 1) return false;
            // 하나의 지대가 부지 전체를 덮어야 한다. 서로 다른 지대의 합집합은 인정하지 않는다.
            foreach (var site in _sites)
            {
                if (site.Kind != kind) continue;
                if (x >= site.X && z >= site.Z && x + width <= site.X + site.Size && z + depth <= site.Z + site.Size)
                    return true;
            }
            return false;
        }

        public static EconomicSiteLayout Generate(int radius, int seed, Func<int, int, bool> land,
            int startX, int startZ, EconomicSiteSettings settings, int startProtectedRadius = 0)
        {
            if (radius < 1) throw new ArgumentOutOfRangeException(nameof(radius));
            if (land == null) throw new ArgumentNullException(nameof(land));
            var layout = new EconomicSiteLayout(radius);
            var reachable = layout.ReachableLand(land, startX, startZ);
            // 별도 난수열이므로 자연물·전투 난수 소비량이 배치에 영향을 주지 않는다.
            var random = new Random(unchecked(seed ^ 0x4D35A917));
            layout.Place(EconomicSiteKind.Fertile, settings.FertileCount, settings.FertileSize,
                settings.MinimumDistance, Math.Max(0, startProtectedRadius), startX, startZ, reachable, random);
            layout.Place(EconomicSiteKind.Leyline, settings.LeylineCount, settings.LeylineSize,
                settings.MinimumDistance, Math.Max(0, startProtectedRadius), startX, startZ, reachable, random);
            return layout;
        }

        private bool[,] ReachableLand(Func<int, int, bool> land, int startX, int startZ)
        {
            var reached = new bool[_cells.GetLength(0), _cells.GetLength(1)];
            if (startX < -_radius || startX > _radius || startZ < -_radius || startZ > _radius || !land(startX, startZ))
                return reached;
            var queue = new Queue<(int X, int Z)>();
            reached[startX + _radius, startZ + _radius] = true;
            queue.Enqueue((startX, startZ));
            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                Visit(point.X - 1, point.Z);
                Visit(point.X + 1, point.Z);
                Visit(point.X, point.Z - 1);
                Visit(point.X, point.Z + 1);
            }
            return reached;

            void Visit(int x, int z)
            {
                if (x < -_radius || x > _radius || z < -_radius || z > _radius ||
                    reached[x + _radius, z + _radius] || !land(x, z)) return;
                reached[x + _radius, z + _radius] = true;
                queue.Enqueue((x, z));
            }
        }

        private void Place(EconomicSiteKind kind, int count, int size, int minimumDistance, int startProtectedRadius,
            int startX, int startZ, bool[,] reachable, Random random)
        {
            if (count == 0 || size > _radius * 2 + 1) return;
            var candidates = new List<(int X, int Z)>();
            // 지도 최외곽 한 줄은 카메라/해안 경계이므로 정착 지대를 두지 않는다.
            for (int z = -_radius + 1; z <= _radius - size; z++)
            for (int x = -_radius + 1; x <= _radius - size; x++)
                candidates.Add((x, z));
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temporary = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = temporary;
            }
            foreach (var candidate in candidates)
            {
                bool valid = true;
                for (int dz = 0; dz < size && valid; dz++)
                for (int dx = 0; dx < size; dx++)
                {
                    int x = candidate.X + dx, z = candidate.Z + dz;
                    long distanceX = x - startX, distanceZ = z - startZ;
                    if (!reachable[x + _radius, z + _radius] || At(x, z) != EconomicSiteKind.None ||
                        (Math.Abs(distanceX) <= startProtectedRadius && Math.Abs(distanceZ) <= startProtectedRadius) ||
                        distanceX * distanceX + distanceZ * distanceZ < (long)minimumDistance * minimumDistance)
                    {
                        valid = false;
                        break;
                    }
                }
                if (!valid) continue;
                var site = new EconomicSite(kind, candidate.X, candidate.Z, size);
                _sites.Add(site);
                for (int dz = 0; dz < size; dz++)
                for (int dx = 0; dx < size; dx++)
                    _cells[candidate.X + dx + _radius, candidate.Z + dz + _radius] = kind;
                if (--count == 0) break;
            }
        }
    }
}
