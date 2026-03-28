using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.World
{
    public class WorldMapGenerator
    {
        private readonly FastNoiseLite fastNoiseLite = new();

        public bool Generate(WorldMapGenerationParameter param, out WorldMap worldMap)
        {
            worldMap = null;
            var mapSize = param.MapSize;
            var width = mapSize.x;
            var height = mapSize.y;
            var area = width * height;

            var index = 0;
            var radialMask = new float[area];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dx = x / (float)width - 0.5f;
                    var dy = y / (float)height - 0.5f;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) * 2;
                    radialMask[index++] = param.RadialMaskFalloff.Evaluate(distance * 1.414f);
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
                        param.ElevationSeaLevel
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

            var layers = new Dictionary<WorldMapLayerType, IWorldMapLayer>()
            {
                [WorldMapLayerType.Terrain] = new TerrainLayer(mapSize, cells),
                [WorldMapLayerType.Field] = new FieldLayer(mapSize, new()),
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

        private GeographyType GetGeographyType(float elevation, float seaLevel)
        {
            if (elevation < seaLevel * 0.5f)
            {
                return GeographyType.DeepWater;
            }
            else if (elevation < seaLevel)
            {
                return GeographyType.ShallowWater;
            }
            else if (elevation < seaLevel + 0.1f)
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
    }
}