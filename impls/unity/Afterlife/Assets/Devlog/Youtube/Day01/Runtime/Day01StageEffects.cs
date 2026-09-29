using Afterlife.Modules.Vegetation;
using Afterlife.Modules.SceneFeatures;
using Afterlife.Modules.Lighting;
using Afterlife.Modules.Interaction;
using System;
using System.Collections.Generic;
using Afterlife.Modules.Hover;
using Afterlife.Devlog.Shoreline;
using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    // All stage-local visual effects have one owner and one disposal point.
    internal sealed class Day01StageEffects : IDisposable
    {
        private readonly Camera _camera;
        private readonly ParticleSystem _smokePrefab;
        private readonly Func<Vector3, Vector3> _wind;
        private readonly Action _waterRipple;
        private readonly List<LeafCardRenderer> _leaves = new List<LeafCardRenderer>();
        private Transform _stage;
        private readonly List<(Renderer Renderer,bool Enabled)> _visibility=new List<(Renderer,bool)>();
        private SmokeEffect _smoke;
        private GrassRenderer _grass;
        private ShowcaseShorelinePreview _shoreline;
        private TerrainHoverEffects _tileHover;
        private ShowcaseHoverTerrain _hoverTerrain;
        private CrystalGlowEffect _crystalGlow;

        public int TreeCount => _leaves.Count;
        public int LeafCount
        {
            get { int count = 0; foreach (var leaves in _leaves) count += leaves.Count; return count; }
        }
        public bool LeavesEnabled => _leaves.Count > 0 && _leaves[0].Enabled;
        public bool LeafRotation => _leaves.Count > 0 && _leaves[0].RotationVariation;
        public ShorelineMode? CurrentShorelineMode => _shoreline == null ? (ShorelineMode?)null : _shoreline.Mode;
        public GrassHoverMode? GrassMode => _tileHover == null ? (GrassHoverMode?)null : _tileHover.GrassMode;

        public Day01StageEffects(Camera camera, ParticleSystem smokePrefab, Func<Vector3, Vector3> wind, Action waterRipple)
        {
            _camera = camera;
            _smokePrefab = smokePrefab;
            _wind = wind;
            _waterRipple = waterRipple;
        }

        public void Create(Transform stage, int phase, ShowcaseTarget[] targets, SceneFeatureSettings settings)
        {
            _stage = stage;
            // 계층 이름 해석은 Day01 어댑터에만 남긴다. 공용 서비스는 명시적인 바인딩을 사용한다.
            foreach(var surface in stage.GetComponentsInChildren<PickSurface>(true)) {
                if(surface.Target!=null || surface.VisibleRenderer==null)continue;
                bool terrain=surface.name.StartsWith("Tile_") || surface.name=="OceanBlocks_HalfHeight";
                if(!terrain)continue;var local=stage.InverseTransformPoint(surface.transform.position);
                bool water=surface.name=="OceanBlocks_HalfHeight" || ShowcaseBiomeMap.At(Mathf.FloorToInt(local.x),Mathf.FloorToInt(local.z))==Afterlife.Modules.Terrain.TerrainBiome.Water;
                _visibility.Add((surface.VisibleRenderer,surface.VisibleRenderer.enabled));surface.VisibleRenderer.enabled &= settings.IsEnabled(water?SceneFeature.Water:SceneFeature.Terrain);
            }
            if(!settings.IsEnabled(SceneFeature.Decorations))foreach(var renderer in stage.GetComponentsInChildren<Renderer>(true))
                if(renderer.name.Contains("Flower") || renderer.name.Contains("Fern")) {_visibility.Add((renderer,renderer.enabled));renderer.enabled=false;}
            if (phase >= 3)
            {
                if(settings.IsEnabled(SceneFeature.Smoke)) _smoke = new SmokeEffect(stage, FindChimneyOutlet, _smokePrefab, _wind);
                if(settings.IsEnabled(SceneFeature.Grass)) _grass = new GrassRenderer(stage, new GrassRenderer.StageLayout
                {
                    MinX = -6, MinZ = -5, Width = 12, Depth = 10,
                    EnvironmentPath = "Environment", ResourcesPath = "ClickableResources", GridPath = "Terrain_Flat/BiomeGrid_12x10",
                    Biome = ShowcaseBiomeMap.At, EnableDecorations=settings.IsEnabled(SceneFeature.Decorations),
                    DecorationFlower = decoration =>
                        decoration.name == "Flowers_Pivot" || decoration.name.StartsWith("TileDecoration_Flowers_") ? true :
                        decoration.name == "Fern_Pivot" || decoration.name.StartsWith("TileDecoration_Ferns_") ? false : (bool?)null
                });
                _grass?.SetSizeScale(settings.GrassSize);
                foreach (var target in targets)
                {
                    if (!settings.IsEnabled(SceneFeature.Leaves) || !target.Wood || !target.transform.IsChildOf(stage)) continue;
                    var surfaces = new List<LeafCardRenderer.Surface>();
                    foreach (var surface in target.Visual.GetComponentsInChildren<PickSurface>(true))
                    {
                        if (surface.SnowCap || surface.Mesh == null) continue;
                        surfaces.Add(new LeafCardRenderer.Surface
                        {
                            Transform = surface.transform, Vertices = surface.Mesh.Vertices,
                            Triangles = surface.Mesh.Triangles, Normals = surface.Mesh.Normals,
                            Colors = surface.Mesh.Colors, Renderer = surface.VisibleRenderer
                        });
                    }
                    var leaves = new LeafCardRenderer(surfaces, () => target.RenderVersion,maxCards:settings.LeafCards);
                    _leaves.Add(leaves);
                    Debug.Log("Leaf sprites: " + target.name + " / " + leaves.Count + " cards.", target);
                }
            }
            if (phase >= 2 && settings.IsEnabled(SceneFeature.Coast))
            {
                _shoreline = new ShowcaseShorelinePreview(stage);
                if (phase == 5) _shoreline.Select(ShorelineMode.Rounded);
            }
            if (phase >= 4 && settings.IsEnabled(SceneFeature.TerrainReaction))
            {
                _tileHover = new TerrainHoverEffects(stage, new TerrainHoverEffects.Map
                { MinX = -6, MinZ = -5, Width = 12, Depth = 10, Biome = ShowcaseBiomeMap.At });
                if (phase == 5) _tileHover.GrassMode = GrassHoverMode.Sway;
                _tileHover.WaterRippleCreated += _waterRipple;
                _hoverTerrain = new ShowcaseHoverTerrain(stage, _tileHover);
            }
            if (phase == 5 && settings.IsEnabled(SceneFeature.Bloom)) _crystalGlow = new CrystalGlowEffect(stage, _camera);
        }

        public bool SelectShoreline(ShorelineMode mode, bool resetHover = true)
        {
            if (_shoreline == null || _shoreline.Mode == mode) return false;
            _hoverTerrain?.Dispose();
            _hoverTerrain = null;
            _shoreline.Select(mode);
            if (_tileHover != null)
            {
                if (resetHover) _tileHover.Reset();
                _hoverTerrain = new ShowcaseHoverTerrain(_stage, _tileHover);
            }
            return true;
        }

        public void SetGrassMode(GrassHoverMode mode, bool resetHover = true)
        {
            if (_tileHover == null) return;
            if (resetHover) _tileHover.Reset();
            _tileHover.GrassMode = mode;
        }

        public void ToggleLeaves() { foreach (var leaves in _leaves) leaves.Toggle(); }
        public void ToggleLeafRotation() { foreach (var leaves in _leaves) leaves.ToggleRotation(); }
        public void ClearPointer() => _tileHover?.SetPointer(null);
        public void SetPointer(Vector3 point, Afterlife.Modules.Terrain.TerrainBiome biome, bool moving) => _tileHover?.SetPointer(point, biome, moving);
        public bool HasGrassAt(Vector3 point) => _grass != null && _grass.HasGrassAt(point);
        public void Tick(float deltaTime) { _smoke?.TickWind(); _tileHover?.Tick(deltaTime); }
        public void LateTick() { foreach (var leaves in _leaves) leaves.Tick(_camera); _grass?.Tick(_camera); }
        public void ResetTake() { _tileHover?.Reset(); _smoke?.ResetTake(); }

        public void Dispose()
        {
            if (_tileHover != null) _tileHover.WaterRippleCreated -= _waterRipple;
            _hoverTerrain?.Dispose(); _hoverTerrain = null;
            _tileHover?.Dispose(); _tileHover = null;
            _shoreline?.Dispose(); _shoreline = null;
            foreach (var leaves in _leaves) leaves.Dispose();
            _leaves.Clear();
            _grass?.Dispose(); _grass = null;
            _crystalGlow?.Dispose(); _crystalGlow = null;
            _smoke?.Dispose(); _smoke = null;
            foreach(var item in _visibility)if(item.Renderer!=null)item.Renderer.enabled=item.Enabled;_visibility.Clear();
            _stage = null;
        }

        private static Vector3? FindChimneyOutlet(Transform house)
        {
            var surfaces = house.GetComponentsInChildren<PickSurface>(true);
            float highest = float.NegativeInfinity;
            foreach (var surface in surfaces)
                if (surface.Mesh != null && surface.Mesh.Vertices != null)
                    foreach (var vertex in surface.Mesh.Vertices)
                        highest = Mathf.Max(highest, surface.transform.TransformPoint(vertex).y);
            if (float.IsNegativeInfinity(highest)) return null;
            Vector3 total = Vector3.zero;
            var unique = new HashSet<Vector3>();
            foreach (var surface in surfaces)
                if (surface.Mesh != null && surface.Mesh.Vertices != null)
                    foreach (var vertex in surface.Mesh.Vertices)
                    {
                        Vector3 world = surface.transform.TransformPoint(vertex);
                        if (highest - world.y < .001f && unique.Add(world)) total += world;
                    }
            return unique.Count > 0 ? total / unique.Count : (Vector3?)null;
        }
    }
}
