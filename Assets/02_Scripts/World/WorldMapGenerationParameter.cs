using UnityEngine;

namespace Afterlife.Dev
{
    [CreateAssetMenu(fileName = "WorldMapGenerationParameter", menuName = "Afterlife/World Map Generation Parameter")]
    public class WorldMapGenerationParameter : ScriptableObject
    {
        public Vector2Int MapSize;

        [Header("Radial Mask")]
        public AnimationCurve RadialMaskFalloff = AnimationCurve.Linear(0, 1, 1, 0);

        [Header("Elevation")]
        public FastNoiseLite.NoiseType ElevationNoiseType = FastNoiseLite.NoiseType.Perlin;
        public int ElevationGenerationSeed; // 0 is random
        public float ElevationGenerationFrequency = 0.01f;
        public float ElevationExponent = 1f;
        public float ElevationSeaLevel = 0.5f;

        [Header("Temperature")]
        public FastNoiseLite.NoiseType TemperatureNoiseType = FastNoiseLite.NoiseType.Perlin;
        public int TemperatureGenerationSeed; // 0 is random
        public float TemperatureGenerationFrequency = 0.01f;
        public float[] TemperatureThresholds = new float[] { 0.3f, 0.6f };

        [Header("Moisture")]
        public FastNoiseLite.NoiseType MoistureNoiseType = FastNoiseLite.NoiseType.Perlin;
        public int MoistureGenerationSeed; // 0 is random
        public float MoistureGenerationFrequency = 0.01f;
        public float[] MoistureThresholds = new float[] { 0.3f, 0.6f };
    }
}