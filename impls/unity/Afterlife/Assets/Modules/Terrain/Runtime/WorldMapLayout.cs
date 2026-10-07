using System;
using System.Collections.Generic;

namespace Afterlife.Modules.Terrain
{
    // 시드 기반 지형/수계/육지 경로 생성. 렌더링 및 게임 개체와 독립적이다.
    public sealed class WorldMapLayout
    {
        public readonly int Radius, Size, Seed;
        public readonly TerrainBiome[] Tiles;
        public readonly bool[] Roads;
        public readonly List<(int X, int Z)> Landmarks = new List<(int, int)>();
        private readonly Random _random;

        public WorldMapLayout(int radius, int seed, bool beach = true)
        {
            Radius = radius;
            Size = radius * 2 + 1;
            Seed = seed;
            Tiles = new TerrainBiome[Size * Size];
            Roads = new bool[Tiles.Length];
            _random = new Random(seed);

            PaintContinent();
            ProtectStartArea();
            RemoveDisconnectedLand();
            PaintBiomes();
            if (Radius >= 16)
            {
                AddWaterway();
                if (Radius >= 35) AddWaterway();
            }
            ProtectStartArea();
            if(beach) AddShore();
            AddLandmarks();
            EnsureResourceBiome(TerrainBiome.Forest);
            EnsureResourceBiome(TerrainBiome.Rocky);
        }

        private void PaintContinent()
        {
            double angle = _random.NextDouble() * Math.PI * 2;
            double cosine = Math.Cos(angle), sine = Math.Sin(angle);
            double axisX = Radius * (.67 + _random.NextDouble() * .40);
            double axisZ = Radius * (.53 + _random.NextDouble() * .42);
            double centerX = (_random.NextDouble() - .5) * Radius * .27;
            double centerZ = (_random.NextDouble() - .5) * Radius * .27;
            int lobeCount = 1 + _random.Next(4);
            var lobeAngles = new double[lobeCount];
            var lobeStrengths = new double[lobeCount];
            var lobePowers = new int[lobeCount];
            for (int i = 0; i < lobeCount; i++)
            {
                lobeAngles[i] = _random.NextDouble() * Math.PI * 2;
                lobeStrengths[i] = (_random.NextDouble() - .26) * .39;
                lobePowers[i] = 2 + _random.Next(5);
            }
            double broadPhase = _random.NextDouble() * Math.PI * 2;
            double broadStrength = .055 + _random.NextDouble() * .11;
            double phase = _random.NextDouble() * 100;

            for (int z = -Radius; z <= Radius; z++)
            for (int x = -Radius; x <= Radius; x++)
            {
                double dx = x - centerX, dz = z - centerZ;
                double localX = (dx * cosine + dz * sine) / axisX;
                double localZ = (-dx * sine + dz * cosine) / axisZ;
                double bearing = Math.Atan2(dz, dx);
                double lobes = broadStrength * Math.Cos(bearing * 2 + broadPhase);
                for (int i = 0; i < lobeCount; i++)
                    lobes += lobeStrengths[i] * Math.Pow(Math.Max(0, Math.Cos(bearing - lobeAngles[i])), lobePowers[i]);
                double contour = Math.Sqrt(localX * localX + localZ * localZ)
                    - lobes
                    + (Noise(x / (Radius * .42) + phase, z / (Radius * .42)) - .5) * .25
                    + (Noise(x / (Radius * .13) - phase, z / (Radius * .13) + phase) - .5) * .075;
                Tiles[Index(x, z)] = Playable(x, z) && contour <= 1
                    ? TerrainBiome.Grass : TerrainBiome.Water;
            }
        }

        private void PaintBiomes()
        {
            double climateAngle = _random.NextDouble() * Math.PI * 2;
            double snowX = Math.Cos(climateAngle), snowZ = Math.Sin(climateAngle);
            double phase = _random.NextDouble() * 100;
            for (int z = -Radius; z <= Radius; z++)
            for (int x = -Radius; x <= Radius; x++)
            {
                int index = Index(x, z);
                if (Tiles[index] != TerrainBiome.Grass || IsProtected(x, z)) continue;
                double latitude = x * snowX + z * snowZ;
                double limit = Radius * (.39 + (Noise(x * .10 + phase, z * .10) - .5) * .34);
                if (latitude > limit) Tiles[index] = TerrainBiome.Snow;
            }

            for (int i = 0; i < 12; i++)
            {
                TerrainBiome biome = i < 7 ? TerrainBiome.Forest : TerrainBiome.Rocky;
                for (int attempt = 0; attempt < 64; attempt++)
                {
                    int x = Range(-Radius + 2, Radius - 2);
                    int z = Range(-Radius + 2, Radius - 2);
                    if (Tiles[Index(x, z)] != TerrainBiome.Grass || IsProtected(x, z) || !DryArea(x, z))
                        continue;
                    double spread = Math.Max(2, Radius * (biome == TerrainBiome.Forest ? .10 : .075));
                    PaintPatch(x, z, spread, biome);
                    break;
                }
            }
        }

        private void PaintPatch(int cx, int cz, double radius, TerrainBiome biome)
        {
            for (int z = Math.Max(-Radius, cz - (int)radius - 2); z <= Math.Min(Radius, cz + (int)radius + 2); z++)
            for (int x = Math.Max(-Radius, cx - (int)radius - 2); x <= Math.Min(Radius, cx + (int)radius + 2); x++)
            {
                if (Tiles[Index(x, z)] != TerrainBiome.Grass || IsProtected(x, z)) continue;
                double edge = radius + (Noise(x * .4, z * .4) - .5) * 2;
                if ((x - cx) * (x - cx) + (z - cz) * (z - cz) < edge * edge)
                    Tiles[Index(x, z)] = biome;
            }
        }

        private void AddWaterway()
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                double angle = _random.NextDouble() * Math.PI * 2;
                double distance = Radius * (.32 + _random.NextDouble() * .22);
                int x = (int)Math.Round(Math.Cos(angle) * distance);
                int z = (int)Math.Round(Math.Sin(angle) * distance);
                if (!Inside(x, z) || IsProtected(x, z) || !DryArea(x, z)) continue;

                double lakeRadius = Math.Max(1.8, Radius * (.055 + _random.NextDouble() * .035));
                int coast = FindCoast(x, z, lakeRadius + 3);
                if (coast < 0) continue;
                var before = (TerrainBiome[])Tiles.Clone();
                int landBefore = CountLand();
                PaintLake(x, z, lakeRadius, _random.NextDouble() * Math.PI * 2,
                    _random.NextDouble() * Math.PI * 2, 1 + _random.NextDouble() * .7);
                int coastX = coast % Size - Radius, coastZ = coast / Size - Radius;
                double length = Math.Sqrt((coastX - x) * (coastX - x) + (coastZ - z) * (coastZ - z));
                int steps = Math.Max(1, (int)Math.Ceiling(length * 3));
                double bend = (_random.NextDouble() - .5) * Math.Min(4, Radius * .08);
                for (int i = 0; i <= steps; i++)
                {
                    double t = i / (double)steps;
                    double offset = Math.Sin(t * Math.PI) * bend;
                    double px = x + (coastX - x) * t - (coastZ - z) / length * offset;
                    double pz = z + (coastZ - z) * t + (coastX - x) / length * offset;
                    PaintWater(px, pz, 1.05 + .18 * Math.Sin(t * 18));
                }
                ProtectStartArea();
                if (OceanWater()[Index(x, z)] && CountLand() >= landBefore * .72
                    && AllLandConnected()) return;
                Array.Copy(before, Tiles, Tiles.Length);
            }
        }

        private int FindCoast(int x, int z, double minimumDistance)
        {
            bool[] ocean = OceanWater();
            int chosen = -1;
            double best = double.PositiveInfinity;
            double outwardLength = Math.Sqrt(x * x + z * z);
            for (int cz = -Radius; cz <= Radius; cz++)
            for (int cx = -Radius; cx <= Radius; cx++)
            {
                int index = Index(cx, cz);
                if (!ocean[index]) continue;
                double dx = cx - x, dz = cz - z;
                double distance = Math.Sqrt(dx * dx + dz * dz);
                if (distance < minimumDistance) continue;
                double outward = (dx * x + dz * z) / Math.Max(1, distance * outwardLength);
                double score = distance * (1.3 - .3 * outward);
                if (score < best) { best = score; chosen = index; }
            }
            return chosen;
        }

        private bool[] OceanWater()
        {
            var ocean = new bool[Tiles.Length];
            var queue = new Queue<int>();
            for (int i = 0; i < Tiles.Length; i++)
            {
                int x = i % Size - Radius, z = i / Size - Radius;
                if (Math.Abs(x) != Radius && Math.Abs(z) != Radius) continue;
                if (Tiles[i] != TerrainBiome.Water) continue;
                ocean[i] = true;
                queue.Enqueue(i);
            }
            while (queue.Count > 0)
                foreach (int next in Neighbors(queue.Dequeue()))
                    if (!ocean[next] && Tiles[next] == TerrainBiome.Water)
                    {
                        ocean[next] = true;
                        queue.Enqueue(next);
                    }
            return ocean;
        }

        private void PaintLake(int cx, int cz, double radius, double phase, double orientation, double aspect)
        {
            int reach = (int)Math.Ceiling(radius * aspect * 1.3);
            double cosine = Math.Cos(orientation), sine = Math.Sin(orientation);
            for (int z = Math.Max(-Radius, cz - reach); z <= Math.Min(Radius, cz + reach); z++)
            for (int x = Math.Max(-Radius, cx - reach); x <= Math.Min(Radius, cx + reach); x++)
            {
                if (!Playable(x, z) || IsProtected(x, z)) continue;
                double bearing = Math.Atan2(z - cz, x - cx);
                double edge = radius * (1 + .13 * Math.Sin(bearing * 3 + phase) + .09 * Math.Cos(bearing * 5));
                double localX = (x - cx) * cosine + (z - cz) * sine;
                double localZ = (z - cz) * cosine - (x - cx) * sine;
                if (localX * localX / (aspect * aspect) + localZ * localZ < edge * edge)
                    Tiles[Index(x, z)] = TerrainBiome.Water;
            }
        }

        private void PaintWater(double px, double pz, double radius)
        {
            for (int z = (int)Math.Floor(pz - radius); z <= (int)Math.Ceiling(pz + radius); z++)
            for (int x = (int)Math.Floor(px - radius); x <= (int)Math.Ceiling(px + radius); x++)
                if (Playable(x, z) && !IsProtected(x, z)
                    && (x - px) * (x - px) + (z - pz) * (z - pz) <= radius * radius)
                    Tiles[Index(x, z)] = TerrainBiome.Water;
        }

        private bool IsProtected(int x, int z) => Math.Abs(x) <= 5 && Math.Abs(z) <= 5;
        private bool Playable(int x, int z) => Math.Abs(x) < Radius && Math.Abs(z) < Radius;

        private void ProtectStartArea()
        {
            for (int z = -Math.Min(5, Radius); z <= Math.Min(5, Radius); z++)
            for (int x = -Math.Min(5, Radius); x <= Math.Min(5, Radius); x++)
                Tiles[Index(x, z)] = TerrainBiome.Grass;
            // 최소 반경에서도 집 주위의 보호 영역 바깥에 자원 바이옴 자리를 남긴다.
            if (Radius <= 10)
                foreach (var point in new[] { (X: 6, Z: 0), (X: -6, Z: 0), (X: 0, Z: 6), (X: 0, Z: -6) })
                    if (Playable(point.X, point.Z)) Tiles[Index(point.X, point.Z)] = TerrainBiome.Grass;
        }

        private int CountLand()
        {
            int count = 0;
            foreach (TerrainBiome tile in Tiles) if (tile != TerrainBiome.Water) count++;
            return count;
        }

        private bool[] Reachable()
        {
            var seen = new bool[Tiles.Length];
            var queue = new Queue<int>();
            int start = Index(0, 0);
            seen[start] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
                foreach (int next in Neighbors(queue.Dequeue()))
                    if (!seen[next] && Tiles[next] != TerrainBiome.Water
                        && Playable(next % Size - Radius, next / Size - Radius))
                    {
                        seen[next] = true;
                        queue.Enqueue(next);
                    }
            return seen;
        }

        private bool AllLandConnected()
        {
            bool[] reachable = Reachable();
            for (int i = 0; i < Tiles.Length; i++)
                if (Tiles[i] != TerrainBiome.Water && !reachable[i]) return false;
            return true;
        }

        private void RemoveDisconnectedLand()
        {
            bool[] reachable = Reachable();
            for (int i = 0; i < Tiles.Length; i++)
                if (!reachable[i]) Tiles[i] = TerrainBiome.Water;
        }

        private void AddShore()
        {
            var old = (TerrainBiome[])Tiles.Clone();
            for (int z = -Radius + 1; z < Radius; z++)
            for (int x = -Radius + 1; x < Radius; x++)
            {
                int index = Index(x, z);
                if (IsProtected(x, z) || old[index] == TerrainBiome.Water || old[index] == TerrainBiome.Snow)
                    continue;
                if (old[index - 1] == TerrainBiome.Water || old[index + 1] == TerrainBiome.Water
                    || old[index - Size] == TerrainBiome.Water || old[index + Size] == TerrainBiome.Water)
                    Tiles[index] = TerrainBiome.Sand;
            }
        }

        private void EnsureResourceBiome(TerrainBiome biome)
        {
            int count = 0;
            foreach (TerrainBiome tile in Tiles) if (tile == biome) count++;
            if (count >= 3) return;

            bool[] reachable = Reachable();
            var candidates = new List<(double Score, int Index)>();
            for (int z = -Radius + 1; z < Radius; z++)
            for (int x = -Radius + 1; x < Radius; x++)
            {
                int index = Index(x, z);
                if (!reachable[index] || Tiles[index] == TerrainBiome.Water
                    || IsProtected(x, z) || Roads[index]) continue;
                double score = (Tiles[index] == TerrainBiome.Grass ? 100 : 0)
                    + (DryArea(x, z) ? 20 : 0)
                    + Math.Min(x * x + z * z, Radius * Radius) * .01
                    + Hash(x, z) * .001;
                candidates.Add((score, index));
            }
            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            foreach (var candidate in candidates)
            {
                if (count >= 3) break;
                int index = candidate.Index;
                if (Tiles[index] == biome) continue;
                // 다른 자원 바이옴을 보충 단계에서 지우지 않는다.
                if (Tiles[index] == TerrainBiome.Forest || Tiles[index] == TerrainBiome.Rocky) continue;
                Tiles[index] = biome;
                count++;
            }
        }

        private void AddLandmarks()
        {
            bool[] reachable = Reachable();
            double minimumDistance = Math.Min(8, Radius * .55);
            for (int i = 0; i < 4; i++)
            {
                int chosen = -1;
                double best = double.NegativeInfinity;
                for (int z = -Radius + 2; z <= Radius - 2; z++)
                for (int x = -Radius + 2; x <= Radius - 2; x++)
                {
                    int index = Index(x, z);
                    if (!reachable[index] || !DryArea(x, z)
                        || x * x + z * z < minimumDistance * minimumDistance) continue;
                    double score = x * x + z * z;
                    foreach (var other in Landmarks)
                    {
                        double separation = (x - other.X) * (x - other.X) + (z - other.Z) * (z - other.Z);
                        if (separation < Math.Max(16, Radius * Radius * .025)) { score = -1; break; }
                        score = Math.Min(score, separation);
                    }
                    if (score < 0) continue;
                    score += Hash(x, z) * .01;
                    if (score > best) { best = score; chosen = index; }
                }
                if (chosen < 0) break;
                int px = chosen % Size - Radius, pz = chosen / Size - Radius;
                if (!CarveRoad(px, pz)) break;
                Landmarks.Add((px, pz));
            }
        }

        private bool DryArea(int x, int z)
        {
            for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
                if (!Inside(x + dx, z + dz) || Tiles[Index(x + dx, z + dz)] == TerrainBiome.Water)
                    return false;
            return true;
        }

        private bool CarveRoad(int targetX, int targetZ)
        {
            int start = Index(0, 0), target = Index(targetX, targetZ);
            var cost = new double[Tiles.Length];
            var previous = new int[Tiles.Length];
            for (int i = 0; i < cost.Length; i++) { cost[i] = double.PositiveInfinity; previous[i] = -1; }
            cost[start] = 0;
            var queue = new SortedSet<(double Cost, int Index)> { (0, start) };
            while (queue.Count > 0)
            {
                var entry = queue.Min;
                queue.Remove(entry);
                int current = entry.Index;
                if (entry.Cost > cost[current]) continue;
                if (current == target) break;
                foreach (int next in Neighbors(current))
                {
                    if (Tiles[next] == TerrainBiome.Water
                        || !Playable(next % Size - Radius, next / Size - Radius)) continue;
                    int x = next % Size - Radius, z = next / Size - Radius;
                    double nextCost = cost[current] + (Roads[next] ? .4 : 1 + Noise(x * .16, z * .16) * 3);
                    if (nextCost >= cost[next]) continue;
                    cost[next] = nextCost;
                    previous[next] = current;
                    queue.Add((nextCost, next));
                }
            }
            if (previous[target] < 0) return false;
            for (int node = target; node != start; node = previous[node])
            {
                int x = node % Size - Radius, z = node / Size - Radius;
                double width = x * x + z * z < Radius * Radius * .13
                    ? 1.5 + Noise(x * .15, z * .15) : .65 + Noise(x * .25, z * .25) * .8;
                for (int dz = -2; dz <= 2; dz++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    int rx = x + dx, rz = z + dz;
                    if (dx * dx + dz * dz > width * width || !Inside(rx, rz) || IsProtected(rx, rz))
                        continue;
                    int adjacent = Index(rx, rz);
                    if (Tiles[adjacent] == TerrainBiome.Water) continue;
                    Roads[adjacent] = true;
                    Tiles[adjacent] = TerrainBiome.Dirt;
                }
            }
            return true;
        }

        private int Range(int min, int max) => max <= min ? min : _random.Next(min, max + 1);
        private int Index(int x, int z) => (z + Radius) * Size + x + Radius;
        private bool Inside(int x, int z) => x >= -Radius && x <= Radius && z >= -Radius && z <= Radius;
        private double Hash(int x, int z) =>
            TerrainTileVariants.Seed(unchecked(x + Seed), unchecked(z - Seed)) / (double)int.MaxValue;
        private double Noise(double x, double z)
        {
            int ix = (int)Math.Floor(x), iz = (int)Math.Floor(z);
            double u = x - ix, v = z - iz;
            u = u * u * (3 - 2 * u);
            v = v * v * (3 - 2 * v);
            return (Hash(ix, iz) * (1 - u) + Hash(ix + 1, iz) * u) * (1 - v)
                + (Hash(ix, iz + 1) * (1 - u) + Hash(ix + 1, iz + 1) * u) * v;
        }
        private IEnumerable<int> Neighbors(int index)
        {
            int x = index % Size, z = index / Size;
            if (x > 0) yield return index - 1;
            if (x < Size - 1) yield return index + 1;
            if (z > 0) yield return index - Size;
            if (z < Size - 1) yield return index + Size;
        }
    }
}
