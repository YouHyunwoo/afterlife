using System.Collections.Generic;
using DataStructure.Heap;
using UnityEngine;

namespace Afterlife.Dev.World
{
    public class WorldMapGenerator
    {
        public bool Generate(WorldMapGenerationParameter param, out WorldMap worldMap)
        {
            worldMap = null;

            var mapSize = param.MapSize;
            var width = mapSize.x;
            var height = mapSize.y;
            var area = width * height;
            var radialScaler = Mathf.Sqrt(2f);

            var index = 0;
            var radialMask = new float[area];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dx = x / (float)width - 0.5f;
                    var dy = y / (float)height - 0.5f;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) * 2;
                    radialMask[index++] = param.RadialMaskFalloff.Evaluate(distance * radialScaler);
                }
            }

            GenerateElevationLayer(param, radialMask, out var elevationLayer);
            GenerateTemperatureLayer(param, out var temperatureLayer);
            GenerateMoistureLayer(param, out var moistureLayer);

            TerrainCell[,] cells = new TerrainCell[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var geographyType = GetGeographyType(
                        elevationLayer[y * width + x],
                        param.ElevationSeaLevel,
                        0.5f,
                        2f
                    );
                    var biomeType = GetBiomeType(
                        temperatureLayer[y * width + x],
                        param.TemperatureThresholds,
                        moistureLayer[y * width + x],
                        param.MoistureThresholds
                    );

                    cells[x, y] = new TerrainCell
                    {
                        Geography = geographyType,
                        Biome = biomeType,
                        Elevation = elevationLayer[y * width + x],
                        Temperature = temperatureLayer[y * width + x],
                        Moisture = moistureLayer[y * width + x],
                    };
                }
            }

            EnsureConnectivity(cells, width, height, param.ElevationSeaLevel, param.MinIslandSize,
                param.BridgeRadius, param.TemperatureThresholds, param.MoistureThresholds);

            var layers = new Dictionary<WorldMapLayerType, IWorldMapLayer>()
            {
                [WorldMapLayerType.Terrain] = new TerrainLayer(mapSize, cells),
                [WorldMapLayerType.Field] = new FieldLayer(mapSize, new()),
                [WorldMapLayerType.TownZone] = new TownZoneLayer(mapSize, new()),
            };

            worldMap = new WorldMap(mapSize, layers);
            return true;
        }

        private void GenerateElevationLayer(WorldMapGenerationParameter param, float[] radialMask, out float[] elevationLayer)
        {
            var mapSize = param.MapSize;
            var width = mapSize.x;
            var height = mapSize.y;
            var area = width * height;

            elevationLayer = new float[area];
            float[] elevationHighFrequencyLayer = new float[area];

            var fastNoiseLite = new FastNoiseLite();
            fastNoiseLite.SetNoiseType(param.ElevationNoiseType);
            fastNoiseLite.SetFrequency(param.ElevationGenerationFrequency);
            fastNoiseLite.SetSeed(GetSeed(param.ElevationGenerationSeed));

            GenerateLayer(fastNoiseLite, width, height, elevationLayer);

            fastNoiseLite.SetNoiseType(param.ElevationNoiseType);
            fastNoiseLite.SetFrequency(param.ElevationGenerationFrequency * param.ElevationHighFrequencyMultiplier);
            fastNoiseLite.SetSeed(GetSeed(param.ElevationGenerationHighFrequencySeed));

            GenerateLayer(fastNoiseLite, width, height, elevationHighFrequencyLayer);

            WeightedSumLayer(elevationLayer, elevationHighFrequencyLayer, param.ElevationHighFrequencyWeight);
            NormalizeLayer(elevationLayer);
            ExponentiateLayer(elevationLayer, param.ElevationExponent);
            ApplyRadialMask(elevationLayer, radialMask);
        }

        private void GenerateTemperatureLayer(WorldMapGenerationParameter param, out float[] temperatureLayer)
        {
            var mapSize = param.MapSize;
            var width = mapSize.x;
            var height = mapSize.y;
            var area = width * height;

            temperatureLayer = new float[area];

            var fastNoiseLite = new FastNoiseLite();
            fastNoiseLite.SetNoiseType(param.TemperatureNoiseType);
            fastNoiseLite.SetFrequency(param.TemperatureGenerationFrequency);
            fastNoiseLite.SetSeed(GetSeed(param.TemperatureGenerationSeed));

            GenerateLayer(fastNoiseLite, width, height, temperatureLayer);
            NormalizeLayer(temperatureLayer);
        }

        private void GenerateMoistureLayer(WorldMapGenerationParameter param, out float[] moistureLayer)
        {
            var mapSize = param.MapSize;
            var width = mapSize.x;
            var height = mapSize.y;
            var area = width * height;

            moistureLayer = new float[area];

            var fastNoiseLite = new FastNoiseLite();
            fastNoiseLite.SetNoiseType(param.MoistureNoiseType);
            fastNoiseLite.SetFrequency(param.MoistureGenerationFrequency);
            fastNoiseLite.SetSeed(GetSeed(param.MoistureGenerationSeed));

            GenerateLayer(fastNoiseLite, width, height, moistureLayer);
            NormalizeLayer(moistureLayer);
        }

        private int GetSeed(int seed)
        {
            return seed == 0 ? Random.Range(int.MinValue, int.MaxValue) : seed;
        }

        private void GenerateLayer(FastNoiseLite fnl, int width, int height, float[] layer)
        {
            var index = 0;
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    layer[index++] = fnl.GetNoise(x, y);
        }

        private void WeightedSumLayer(float[] baseLayer, float[] additionalLayer, float weight)
        {
            for (var i = 0; i < baseLayer.Length; i++)
                baseLayer[i] = baseLayer[i] * (1 - weight) + additionalLayer[i] * weight;
        }

        private void NormalizeLayer(float[] layer, float minValue = -1f, float maxValue = 1f)
        {
            var range = maxValue - minValue;
            if (range <= 0f) return;

            for (var i = 0; i < layer.Length; i++)
                layer[i] = (layer[i] - minValue) / range;
        }

        private void ExponentiateLayer(float[] layer, float scale)
        {
            for (var i = 0; i < layer.Length; i++)
                layer[i] = Mathf.Pow(layer[i], scale);
        }

        private void ApplyRadialMask(float[] layer, float[] radialMask)
        {
            for (var i = 0; i < layer.Length; i++)
                layer[i] *= radialMask[i];
        }

        private GeographyType GetGeographyType(float elevation, float seaLevel, float deepWaterFactor, float beachfactor)
        {
            if (elevation < seaLevel * deepWaterFactor)
            {
                return GeographyType.DeepWater;
            }
            else if (elevation < seaLevel)
            {
                return GeographyType.ShallowWater;
            }
            else if (elevation < seaLevel * beachfactor)
            {
                return GeographyType.Beach;
            }
            else
            {
                return GeographyType.Land;
            }
        }

        private BiomeType GetBiomeType(float temperature, float[] temperatureThresholds, float moisture, float[] moistureThresholds)
        {
            if (temperature < temperatureThresholds[0])
            {
                if (moisture < moistureThresholds[0])
                    return BiomeType.IceSheet;
                else if (moisture < moistureThresholds[1])
                    return BiomeType.Tundra;
                else
                    return BiomeType.BorealForest;
            }
            else if (temperature < temperatureThresholds[1])
            {
                if (moisture < moistureThresholds[0])
                    return BiomeType.Desert;
                else if (moisture < moistureThresholds[1])
                    return BiomeType.Grassland;
                else
                    return BiomeType.Forest;
            }
            else
            {
                if (moisture < moistureThresholds[0])
                    return BiomeType.AridShrubland;
                else if (moisture < moistureThresholds[1])
                    return BiomeType.Savanna;
                else
                    return BiomeType.TropicalRainforest;
            }
        }

        private void EnsureConnectivity(
            TerrainCell[,] cells,
            int width,
            int height,
            float seaLevel,
            int minIslandSize,
            int bridgeRadius,
            float[] temperatureThresholds,
            float[] moistureThresholds)
        {
            bool IsLand(int x, int y) =>
                cells[x, y].Geography == GeographyType.Beach ||
                cells[x, y].Geography == GeographyType.Land;

            // Phase A: Flood-fill로 연결 컴포넌트 탐지
            var regionId = new int[width, height];
            for (var i = 0; i < width; i++)
                for (var j = 0; j < height; j++)
                    regionId[i, j] = -1;

            var regions = new List<List<(int x, int y)>>();
            var dx = new int[] { -1, 1, 0, 0 };
            var dy = new int[] { 0, 0, -1, 1 };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (regionId[x, y] != -1 || !IsLand(x, y)) continue;

                    var id = regions.Count;
                    var region = new List<(int, int)>();
                    var queue = new Queue<(int, int)>();
                    queue.Enqueue((x, y));
                    regionId[x, y] = id;

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        region.Add((cx, cy));

                        for (var d = 0; d < 4; d++)
                        {
                            var nx = cx + dx[d];
                            var ny = cy + dy[d];
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                            if (regionId[nx, ny] != -1 || !IsLand(nx, ny)) continue;
                            regionId[nx, ny] = id;
                            queue.Enqueue((nx, ny));
                        }
                    }

                    regions.Add(region);
                }
            }

            if (regions.Count <= 1) return;

            // 가장 큰 컴포넌트 = mainId
            var mainId = 0;
            for (var i = 1; i < regions.Count; i++)
                if (regions[i].Count > regions[mainId].Count)
                    mainId = i;

            // Phase B: 각 고립 지역 처리
            for (var i = 0; i < regions.Count; i++)
            {
                if (i == mainId) continue;

                if (regions[i].Count < minIslandSize)
                    SinkRegion(regions[i], cells, seaLevel);
                else
                    BuildBridge(regions[i], mainId, regionId, cells, width, height, seaLevel,
                        bridgeRadius, temperatureThresholds, moistureThresholds, dx, dy);
            }

            // 브리지 확장으로 새로 생긴 고립 셀 정리
            SinkOrphanedCells(cells, width, height, seaLevel, dx, dy);
        }

        private void SinkOrphanedCells(TerrainCell[,] cells, int width, int height, float seaLevel, int[] dx, int[] dy)
        {
            bool IsLand(int x, int y) =>
                cells[x, y].Geography == GeographyType.Beach ||
                cells[x, y].Geography == GeographyType.Land;

            var visited = new bool[width, height];
            var regions = new List<List<(int x, int y)>>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (visited[x, y] || !IsLand(x, y)) continue;

                    var region = new List<(int, int)>();
                    var queue = new Queue<(int, int)>();
                    queue.Enqueue((x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        region.Add((cx, cy));

                        for (var d = 0; d < 4; d++)
                        {
                            var nx = cx + dx[d];
                            var ny = cy + dy[d];
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                            if (visited[nx, ny] || !IsLand(nx, ny)) continue;
                            visited[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }

                    regions.Add(region);
                }
            }

            if (regions.Count <= 1) return;

            var mainId = 0;
            for (var i = 1; i < regions.Count; i++)
                if (regions[i].Count > regions[mainId].Count)
                    mainId = i;

            for (var i = 0; i < regions.Count; i++)
            {
                if (i == mainId) continue;
                SinkRegion(regions[i], cells, seaLevel);
            }
        }

        private void SinkRegion(List<(int x, int y)> region, TerrainCell[,] cells, float seaLevel)
        {
            foreach (var (x, y) in region)
            {
                cells[x, y].Elevation = seaLevel * 0.4f;
                cells[x, y].Geography = GeographyType.ShallowWater;
            }
        }

        private void BuildBridge(
            List<(int x, int y)> island,
            int mainId,
            int[,] regionId,
            TerrainCell[,] cells,
            int width,
            int height,
            float seaLevel,
            int bridgeRadius,
            float[] temperatureThresholds,
            float[] moistureThresholds,
            int[] dx,
            int[] dy)
        {
            bool IsLand(int x, int y) =>
                cells[x, y].Geography == GeographyType.Beach ||
                cells[x, y].Geography == GeographyType.Land;

            var cost = new float[width, height];
            var parent = new (int x, int y)?[width, height];
            var pq = new PriorityQueue<(int x, int y)>();

            for (var i = 0; i < width; i++)
                for (var j = 0; j < height; j++)
                    cost[i, j] = float.MaxValue;

            // 섬의 경계 물 셀에서 멀티소스 출발
            foreach (var (ix, iy) in island)
            {
                for (var d = 0; d < 4; d++)
                {
                    var nx = ix + dx[d];
                    var ny = iy + dy[d];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    if (IsLand(nx, ny)) continue;
                    var c = seaLevel - cells[nx, ny].Elevation;
                    if (c < cost[nx, ny])
                    {
                        cost[nx, ny] = c;
                        parent[nx, ny] = (ix, iy);
                        pq.Enqueue((nx, ny), c);
                    }
                }
            }

            // Dijkstra: 얕은 물 우선 탐색
            (int x, int y)? found = null;
            while (pq.Count > 0 && found == null)
            {
                var (cx, cy) = pq.Dequeue();

                for (var d = 0; d < 4; d++)
                {
                    var nx = cx + dx[d];
                    var ny = cy + dy[d];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                    if (regionId[nx, ny] == mainId)
                    {
                        found = (cx, cy);
                        break;
                    }

                    if (IsLand(nx, ny)) continue;

                    var newCost = cost[cx, cy] + (seaLevel - cells[nx, ny].Elevation);
                    if (newCost < cost[nx, ny])
                    {
                        cost[nx, ny] = newCost;
                        parent[nx, ny] = (cx, cy);
                        pq.Enqueue((nx, ny), newCost);
                    }
                }
            }

            if (found == null) return;

            // 경로 역추적 — 경로 셀 수집
            var pathCells = new List<(int x, int y)>();
            var current = found;
            while (current.HasValue && !IsLand(current.Value.x, current.Value.y))
            {
                pathCells.Add(current.Value);
                current = parent[current.Value.x, current.Value.y];
            }

            // 경로 셀 → Beach로 변환
            foreach (var (px, py) in pathCells)
            {
                cells[px, py].Elevation = seaLevel * 1.1f;
                cells[px, py].Geography = GeographyType.Beach;
                cells[px, py].Biome = InheritBiome(px, py, cells, width, height,
                    temperatureThresholds, moistureThresholds, dx, dy);
                regionId[px, py] = mainId;
            }

            // BridgeRadius 확장 — 경로 주변 물 셀도 Beach로 변환
            if (bridgeRadius <= 0) return;

            var expanded = new HashSet<(int, int)>(pathCells);
            foreach (var (px, py) in pathCells)
            {
                for (var oy = -bridgeRadius; oy <= bridgeRadius; oy++)
                {
                    for (var ox = -bridgeRadius; ox <= bridgeRadius; ox++)
                    {
                        var nx = px + ox;
                        var ny = py + oy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        if (IsLand(nx, ny)) continue;
                        if (!expanded.Add((nx, ny))) continue;

                        var dist = Mathf.Sqrt(ox * ox + oy * oy);
                        if (dist > bridgeRadius) continue;

                        var t = dist / bridgeRadius;
                        cells[nx, ny].Elevation = seaLevel * (1.1f - 0.05f * t);
                        cells[nx, ny].Geography = GeographyType.Beach;
                        cells[nx, ny].Biome = cells[px, py].Biome;
                        regionId[nx, ny] = mainId;
                    }
                }
            }
        }

        private BiomeType InheritBiome(
            int x, int y,
            TerrainCell[,] cells,
            int width, int height,
            float[] temperatureThresholds,
            float[] moistureThresholds,
            int[] dx, int[] dy)
        {
            for (var d = 0; d < 4; d++)
            {
                var nx = x + dx[d];
                var ny = y + dy[d];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                var geo = cells[nx, ny].Geography;
                if (geo == GeographyType.Beach || geo == GeographyType.Land)
                    return cells[nx, ny].Biome;
            }
            return GetBiomeType(cells[x, y].Temperature, temperatureThresholds,
                cells[x, y].Moisture, moistureThresholds);
        }
    }
}