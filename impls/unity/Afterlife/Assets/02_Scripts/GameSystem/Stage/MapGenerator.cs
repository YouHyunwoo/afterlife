using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public static class MapGenerator
    {
        public static void Generate(Model.Stage stage)
        {
            var mapSize = stage.Map.Size;
            var mapData = stage.Data.MapData;
            var terrain = stage.Map.Terrain;

            var fnl = new FastNoiseLite();
            fnl.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            fnl.SetFractalType(FastNoiseLite.FractalType.FBm);
            fnl.SetFrequency(mapData.GenerationFrequency);
            fnl.SetFractalOctaves(mapData.GenerationOctaves);
            fnl.SetSeed(Random.Range(0, int.MaxValue));

            var radialMask = new float[mapSize.x * mapSize.y];
            var index = 0;
            for (var y = 0; y < mapSize.y; y++)
            {
                for (var x = 0; x < mapSize.x; x++)
                {
                    var dx = x - mapSize.x / 2f;
                    var dy = y - mapSize.y / 2f;
                    var value = mapData.GenerationRadius - Mathf.Sqrt(dx * dx + dy * dy);
                    radialMask[index++] = Mathf.Clamp01(value / mapData.GenerationRadius);
                }
            }

            var heightMap = new float[mapSize.x * mapSize.y];
            index = 0;
            for (var y = 0; y < mapSize.y; y++)
            {
                for (var x = 0; x < mapSize.x; x++)
                {
                    var noiseValue = fnl.GetNoise(x, y);
                    var height = (noiseValue + 1) * 0.5f;
                    height = Mathf.Pow(height, mapData.GenerationScale);
                    height *= radialMask[index];
                    height = height > mapData.GenerationHeightThreshold ? 1f : 0f;
                    heightMap[index++] = height;
                }
            }

            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    var isLand = heightMap[y * mapSize.x + x] > 0;
                    var tileIndex = isLand ? 1 : 3;
                    terrain.TerrainGrid[x, y] = tileIndex;
                    terrain.PassableGrid[x, y] = isLand;
                }
            }
        }
    }
}
