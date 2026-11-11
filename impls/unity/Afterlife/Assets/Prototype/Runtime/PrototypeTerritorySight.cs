using Afterlife.TerritoryVision;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        private readonly TerritorySight _territorySight = new TerritorySight();
        private int _territorySightRevision = -1;
        public TerritorySight TerritorySight
        {
            get
            {
                if (_territorySightRevision == TerritoryRevision) return _territorySight;
                _territorySight.Clear();
                foreach (int key in Territory) _territorySight.AddTile(key / 256 - 128, key % 256 - 128);
                _territorySightRevision = TerritoryRevision;
                return _territorySight;
            }
        }
    }

    public sealed partial class PrototypeView
    {
        private Texture2D _territorySightTexture;
        private Color32[] _territorySightPixels;
        private int _territorySightRevision = -1;
        private VillageModel _territorySightModel;
        private const int TerritorySightPixelsPerTile = 8;

        private void UpdateTerritorySight()
        {
            if (_territorySightModel == _model && _territorySightRevision == _model.TerritoryRevision
                && _territorySightTexture != null) return;
            int size = FogSize * TerritorySightPixelsPerTile;
            if (_territorySightTexture == null || _territorySightTexture.width != size)
            {
                DisposeTerritorySight();
                _territorySightTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
                { name = "Territory sight and light", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                _territorySightPixels = new Color32[size * size];
            }
            var sight = _model.TerritorySight;
            float range = _settings.MapRadius + .5f;
            var coverage = new byte[size * size];
            for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
            {
                float wx = -range + (x + .5f) / TerritorySightPixelsPerTile;
                float wz = -range + (z + .5f) / TerritorySightPixelsPerTile;
                coverage[z * size + x] = (byte)(sight.Contains(wx, wz) ? 255 : 0);
            }
            var illumination = TerritoryEdgeLighting.Create(coverage, size, size, TerritorySightPixelsPerTile);
            for (int i = 0; i < coverage.Length; i++)
                _territorySightPixels[i] = new Color32(coverage[i], illumination[i], 0, 255);
            _territorySightTexture.SetPixels32(_territorySightPixels);
            _territorySightTexture.Apply(false, false);
            Shader.SetGlobalTexture("_PrototypeTerritorySight", _territorySightTexture);
            _territorySightModel = _model;
            _territorySightRevision = _model.TerritoryRevision;
        }

        private void DisposeTerritorySight()
        {
            Shader.SetGlobalTexture("_PrototypeTerritorySight", Texture2D.blackTexture);
            if (_territorySightTexture != null) Destroy(_territorySightTexture);
            _territorySightTexture = null;
            _territorySightPixels = null;
            _territorySightModel = null;
            _territorySightRevision = -1;
        }
    }
}
