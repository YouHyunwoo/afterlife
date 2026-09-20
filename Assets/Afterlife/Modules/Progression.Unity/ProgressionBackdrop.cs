using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Progression
{
    // 외부 이미지 의존성 없는 물방울/성소 배경. 트리 이동과 독립적으로 움직인다.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ProgressionBackdrop : MaskableGraphic
    {
        public bool Laboratory;
        private Texture2D _atlas;
        private const int AtlasColumns = 6, AtlasRows = 5, ParticleCount = DepthLayers * ParticlesPerLayer;
        private readonly Color[] _tilePixels = new Color[CrystalResolution * CrystalResolution];
        private readonly bool[] _ready = new bool[ParticleCount];
        private int _nextCrystal;
        private float _nextBake;
        public override Texture mainTexture => _atlas != null ? _atlas : base.mainTexture;
        private readonly Vector3[] _crystalVertices = new Vector3[6];
        private const int CrystalResolution = 40;
        private readonly Color[] _crystalPixels = new Color[CrystalResolution * CrystalResolution];
        private readonly Color[] _crystalBlur = new Color[CrystalResolution * CrystalResolution];
        private const int DepthLayers = 3, ParticlesPerLayer = 8;
        // 먼 층부터 그린다. 가장 가까운 층도 최소 블러를 유지한다.
        private static readonly float[][] DepthBlurWeights = {
            CreateBlurWeights(2.4f), CreateBlurWeights(1.35f), CreateBlurWeights(.45f)
        };
        private static readonly Color[] CrystalPalette = {
            new Color32(38, 123, 120, 255), new Color32(122, 214, 196, 255),
            new Color32(121, 101, 155, 255), new Color32(191, 145, 69, 255),
            new Color32(237, 204, 121, 255), new Color32(105, 143, 166, 255)
        };
        private static readonly Vector3[] CrystalShape = {
            new Vector3(0, 1, 0), new Vector3(0, -1, 0),
            new Vector3(.44f, 0, 0), new Vector3(0, 0, .38f),
            new Vector3(-.44f, 0, 0), new Vector3(0, 0, -.38f)
        };
        private static readonly int[] CrystalFaces = { 0,2,3, 0,3,4, 0,4,5, 0,5,2, 1,3,2, 1,4,3, 1,5,4, 1,2,5 };
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        private void Update()
        {
            EnsureAtlas();
            if (!Laboratory && Time.unscaledTime >= _nextBake)
            {
                // 24개를 동시에 갱신하지 않고 4개씩 분산한다. 이동은 매 프레임 유지한다.
                _nextBake = Time.unscaledTime + 1f / 60;
                for (int batch = 0; batch < 4; batch++)
                {
                    int i = _nextCrystal;
                    float clock = Time.unscaledTime;
                    BakeCrystal(Quaternion.Euler(clock * 7 + i * 31, clock * 13 + i * 67, clock * 4 + i * 19),
                        CrystalPalette[i % CrystalPalette.Length], DepthBlurWeights[i / ParticlesPerLayer]);
                    StoreTile(i, _tilePixels);
                    _ready[i] = true;
                    _nextCrystal = (i + 1) % ParticleCount;
                }
                _atlas.Apply(false, false);
            }
            SetVerticesDirty();
        }
        private void EnsureAtlas()
        {
            if (_atlas != null) return;
            _atlas = new Texture2D(AtlasColumns * CrystalResolution, AtlasRows * CrystalResolution, TextureFormat.RGBA32, false);
            _atlas.name = "ProgressionBackdropAtlas";
            _atlas.hideFlags = HideFlags.HideAndDontSave;
            _atlas.filterMode = FilterMode.Bilinear;
            _atlas.wrapMode = TextureWrapMode.Clamp;
            _atlas.SetPixels32(new Color32[_atlas.width * _atlas.height]);
            for (int layer = 0; layer < DepthLayers; layer++)
            {
                float near = layer / (float)(DepthLayers - 1);
                BakeBubble(near == 1 ? .055f : Mathf.Lerp(.24f, .09f, near));
                StoreTile(ParticleCount + layer, _tilePixels);
            }
            _atlas.Apply(false, false);
            SetMaterialDirty();
        }
        protected override void OnDestroy()
        {
            if (_atlas != null) Destroy(_atlas);
            base.OnDestroy();
        }
        private void StoreTile(int tile, Color[] pixels)
        {
            _atlas.SetPixels(tile % AtlasColumns * CrystalResolution, tile / AtlasColumns * CrystalResolution,
                CrystalResolution, CrystalResolution, pixels);
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_atlas == null) return;
            var rect = rectTransform.rect;
            float clock = Time.unscaledTime;
            if (Laboratory)
            {
                if (rect.width <= 0 || rect.height <= 0) return;
                for (int i = 0; i < DepthLayers * ParticlesPerLayer; i++)
                {
                    float near = (i / ParticlesPerLayer) / (float)(DepthLayers - 1);
                    float perspective = Mathf.Lerp(.5f, 1.2f, near);
                    float radius = (28 + i * 17 % 9) * perspective;
                    float travel = rect.height + radius * 3;
                    float speed = (10 + (i * 7 % 23) * 1.3f) * perspective;
                    float progress = Mathf.Repeat(i * .173f + clock * speed / travel, 1);
                    float x = Mathf.Lerp(rect.xMin, rect.xMax, .09f + Mathf.Repeat(i * .381966f, .82f));
                    x += Mathf.Sin(clock * .24f + i * 2.1f) * 12 * perspective;
                    float y = rect.yMin - radius * 1.5f + progress * travel;
                    float fade = Mathf.SmoothStep(0, 1, progress / .12f) * Mathf.SmoothStep(0, 1, (1 - progress) / .12f);
                    DrawTile(mesh, ParticleCount + i / ParticlesPerLayer, new Vector2(x, y), radius * 2.8f, fade * Mathf.Lerp(.6f, 1, near));
                }
            }
            else
            {
                if (rect.width <= 0 || rect.height <= 0) return;
                for (int i = 0; i < DepthLayers * ParticlesPerLayer; i++)
                {
                    int depthLayer = i / ParticlesPerLayer;
                    float near = depthLayer / (float)(DepthLayers - 1);
                    float perspective = Mathf.Lerp(.5f, 1.2f, near);
                    float size = (28 + i * 17 % 9) * perspective;
                    float travel = rect.height + size * 3;
                    float progress = Mathf.Repeat(i * .137f + clock * (13 + i % 7 * 2) * perspective / travel, 1);
                    float x = rect.xMin + rect.width * (.06f + Mathf.Repeat(i * .381966f, .88f));
                    x += Mathf.Sin(clock * .18f + i) * 18 * perspective;
                    float y = rect.yMax + size * 1.5f - progress * travel;
                    float fade = Mathf.SmoothStep(0, 1, progress / .08f) * Mathf.SmoothStep(0, 1, (1 - progress) / .08f);
                    if (_ready[i]) DrawTile(mesh, i, new Vector2(x, y), size * CrystalResolution / 12f, fade * Mathf.Lerp(.6f, 1, near));
                }
            }
        }
        private void BakeBubble(float blurWidth)
        {
            for (int y = 0; y < CrystalResolution; y++)
            for (int x = 0; x < CrystalResolution; x++)
            {
                Vector2 position = new Vector2(x + .5f - CrystalResolution * .5f, y + .5f - CrystalResolution * .5f) * (2.8f / CrystalResolution);
                float distance = position.magnitude;
                float rim = Mathf.Exp(-Mathf.Pow((distance - .86f) / blurWidth, 2));
                float fill = .045f * Mathf.Clamp01(1 - distance / 1.25f);
                float edgeFade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.15f, 1.4f, distance));
                float highlight = Mathf.Pow(Mathf.Clamp01(Vector2.Dot(position.normalized, new Vector2(-.6f, .8f))), 6);
                _tilePixels[y * CrystalResolution + x] = new Color(.48f, .84f, .80f, (fill + rim * (.14f + highlight * .10f)) * edgeFade);
            }
        }
        private static void DrawTile(VertexHelper mesh, int tile, Vector2 center, float size, float opacity)
        {
            int start = mesh.currentVertCount;
            float half = size * .5f;
            float u = (tile % AtlasColumns) / (float)AtlasColumns, v = (tile / AtlasColumns) / (float)AtlasRows;
            float right = u + 1f / AtlasColumns, top = v + 1f / AtlasRows;
            Color tint = new Color(1, 1, 1, opacity);
            mesh.AddVert(center + new Vector2(-half, -half), tint, new Vector2(u, v));
            mesh.AddVert(center + new Vector2(-half, half), tint, new Vector2(u, top));
            mesh.AddVert(center + new Vector2(half, half), tint, new Vector2(right, top));
            mesh.AddVert(center + new Vector2(half, -half), tint, new Vector2(right, v));
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
        private void BakeCrystal(Quaternion rotation, Color baseColor, float[] blurWeights)
        {
            for (int i = 0; i < CrystalShape.Length; i++) _crystalVertices[i] = rotation * CrystalShape[i];
            // 회전한 입체의 가장 앞쪽 면을 저해상도 격자에 샘플링한다.
            // 픽셀 원본 바깥에 여백을 두어 블러 윤곽이 잘리지 않게 한다.
            const int resolution = CrystalResolution;
            for (int row = 0; row < resolution; row++)
            for (int column = 0; column < resolution; column++)
            {
                int index = row * resolution + column;
                _crystalPixels[index] = Color.clear;
                Vector2 sample = new Vector2((column + .5f - resolution * .5f) / 12, (row + .5f - resolution * .5f) / 12);
                float nearest = float.NegativeInfinity, shade = 0;
                for (int face = 0; face < CrystalFaces.Length; face += 3)
                {
                    Vector3 a = _crystalVertices[CrystalFaces[face]], b = _crystalVertices[CrystalFaces[face + 1]], c = _crystalVertices[CrystalFaces[face + 2]];
                    float denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
                    if (Mathf.Abs(denominator) < .00001f) continue;
                    float u = ((b.y - c.y) * (sample.x - c.x) + (c.x - b.x) * (sample.y - c.y)) / denominator;
                    float v = ((c.y - a.y) * (sample.x - c.x) + (a.x - c.x) * (sample.y - c.y)) / denominator;
                    if (u < 0 || v < 0 || u + v > 1) continue;
                    float depth = u * a.z + v * b.z + (1 - u - v) * c.z;
                    if (depth <= nearest) continue;
                    nearest = depth;
                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                    if (normal.z < 0) normal = -normal;
                    shade = Mathf.Round(Mathf.Clamp01(Vector3.Dot(normal, new Vector3(-.4f, .6f, .7f).normalized)) * 3) / 3;
                }
                if (float.IsNegativeInfinity(nearest)) continue;
                Color tint = Color.Lerp(baseColor * .48f, Color.Lerp(baseColor, new Color(.95f, .92f, .82f), .24f), shade);
                tint.a = 1;
                // 미리 곱한 알파로 색과 실루엣을 함께 흐려 검은 테두리를 방지한다.
                _crystalPixels[index] = tint * .24f;
            }
            BlurCrystal(_crystalPixels, _crystalBlur, true, blurWeights);
            BlurCrystal(_crystalBlur, _crystalPixels, false, blurWeights);
            for (int index = 0; index < _crystalPixels.Length; index++)
            {
                Color tint = _crystalPixels[index];
                if (tint.a > .00001f) { tint.r /= tint.a; tint.g /= tint.a; tint.b /= tint.a; }
                _tilePixels[index] = tint;
            }
        }
        private static float[] CreateBlurWeights(float sigma)
        {
            // 깊이별 커널을 한 번만 생성한다. 최대 반경 7px는 원본 여백 안에 들어간다.
            var weights = new float[Mathf.Min(7, Mathf.CeilToInt(sigma * 2.8f)) * 2 + 1];
            float total = 0;
            for (int i = 0; i < weights.Length; i++) { float x = i - weights.Length / 2; weights[i] = Mathf.Exp(-x * x / (2 * sigma * sigma)); total += weights[i]; }
            for (int i = 0; i < weights.Length; i++) weights[i] /= total;
            return weights;
        }
        private static void BlurCrystal(Color[] source, Color[] target, bool horizontal, float[] blurWeights)
        {
            for (int y = 0; y < CrystalResolution; y++)
            for (int x = 0; x < CrystalResolution; x++)
            {
                Color sum = Color.clear;
                for (int tap = 0; tap < blurWeights.Length; tap++)
                {
                    int offset = tap - blurWeights.Length / 2;
                    int sx = horizontal ? x + offset : x, sy = horizontal ? y : y + offset;
                    if (sx < 0 || sx >= CrystalResolution || sy < 0 || sy >= CrystalResolution) continue;
                    sum += source[sy * CrystalResolution + sx] * blurWeights[tap];
                }
                target[y * CrystalResolution + x] = sum;
            }
        }
    }
}
