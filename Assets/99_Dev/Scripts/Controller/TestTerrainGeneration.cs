using UnityEngine;

namespace Afterlife.Test
{
    public class TestTerrainGeneration : MonoBehaviour
    {
        public SpriteRenderer radialSpriteRenderer;
        public SpriteRenderer noiseSpriteRenderer;
        public SpriteRenderer radialMaskedNoiseSpriteRenderer;
        public SpriteRenderer terreinSpriteRenderer;
        public bool radialEnabled;
        public bool noiseEnabled;
        public bool radialMaskedNoiseEnabled;
        public bool terreinEnabled;
        public Vector2Int mapSize;
        [Range(0, 1)]
        public float frequency;
        public int octaves;
        public float radius;
        public float scale;
        [Range(0, 1)]
        public float heightThreshold;
        public int seed;

        public bool isDirty;

        void OnValidate()
        {
            radialSpriteRenderer.enabled = radialEnabled;
            noiseSpriteRenderer.enabled = noiseEnabled;
            radialMaskedNoiseSpriteRenderer.enabled = radialMaskedNoiseEnabled;
            terreinSpriteRenderer.enabled = terreinEnabled;

            frequency = Mathf.Max(0, frequency);
            octaves = Mathf.Max(1, octaves);
            radius = Mathf.Max(0, radius);
            heightThreshold = Mathf.Clamp01(heightThreshold);

            isDirty = true;
        }

        void Update()
        {
            if (isDirty)
            {
                isDirty = false;
                GenerateNoise();
            }
        }

        void GenerateNoise()
        {
            var fnl = new FastNoiseLite();
            fnl.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            fnl.SetFractalType(FastNoiseLite.FractalType.FBm);
            fnl.SetFrequency(frequency);
            fnl.SetFractalOctaves(octaves);
            fnl.SetSeed(seed);

            var radialMask = new float[mapSize.x * mapSize.y];
            var index = 0;
            for (var y = 0; y < mapSize.y; y++)
            {
                for (var x = 0; x < mapSize.x; x++)
                {
                    var dx = x - mapSize.x / 2f;
                    var dy = y - mapSize.y / 2f;
                    var value = radius - Mathf.Sqrt(dx * dx + dy * dy);
                    radialMask[index++] = Mathf.Clamp01(value / radius);
                }
            }

            radialSpriteRenderer.sprite = DrawSprite(mapSize, ConvertToColorMap(radialMask));

            var heightMap = new float[mapSize.x * mapSize.y];
            index = 0;
            for (var y = 0; y < mapSize.y; y++)
            {
                for (var x = 0; x < mapSize.x; x++)
                {
                    var noiseValue = fnl.GetNoise(x, y);
                    var height = (noiseValue + 1) * 0.5f;
                    heightMap[index++] = Mathf.Pow(height, scale);
                }
            }

            noiseSpriteRenderer.sprite = DrawSprite(mapSize, ConvertToColorMap(heightMap));

            index = 0;
            for (var y = 0; y < mapSize.y; y++)
            {
                for (var x = 0; x < mapSize.x; x++)
                {
                    var height = heightMap[index];
                    height *= radialMask[index];
                    heightMap[index++] = height;
                }
            }

            radialMaskedNoiseSpriteRenderer.sprite = DrawSprite(mapSize, ConvertToColorMap(heightMap));

            var colorMap = new Color32[mapSize.x * mapSize.y];
            index = 0;
            for (var y = 0; y < mapSize.y; y++)
            {
                for (var x = 0; x < mapSize.x; x++)
                {
                    var heightValue = heightMap[index++] > heightThreshold ? 1f : 0f;
                    colorMap[y * mapSize.x + x] = heightValue > 0 ? Color.white : Color.black;
                }
            }

            terreinSpriteRenderer.sprite = DrawSprite(mapSize, colorMap);
        }

        Color32[] ConvertToColorMap(float[] monochromeColorDatas)
        {
            var colorMap = new Color32[monochromeColorDatas.Length];
            for (int i = 0; i < monochromeColorDatas.Length; i++)
            {
                var value = Mathf.Clamp01(monochromeColorDatas[i]);
                colorMap[i] = new Color32((byte)(value * 255), (byte)(value * 255), (byte)(value * 255), 255);
            }
            return colorMap;
        }

        Sprite DrawSprite(Vector2Int size, Color32[] colorMap)
        {
            var texture = new Texture2D(size.x, size.y)
            {
                filterMode = FilterMode.Point
            };
            texture.SetPixels32(colorMap);
            texture.Apply();

            var rect = new Rect(0, 0, size.x, size.y);
            var sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f);
            return sprite;
        }
    }
}