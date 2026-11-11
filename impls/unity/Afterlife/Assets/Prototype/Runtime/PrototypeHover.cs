using Afterlife.Modules.Hover;
using Afterlife.Modules.Terrain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private TerrainHoverEffects _tileHover;
        [SerializeField] private ParticleSystem _hoverDirtDust;
        private System.Func<Vector3,float> _hoverHeightOffset;
        private Vector2 _hoverPreviousScreen;
        private bool _hoverHadPointer;

        private void InitializeHover()
        {
            if (_hoverDirtDust == null) throw new System.InvalidOperationException("Assign HoverDirtDust prefab instance in GameRoot.");
            _waterSurface?.SetHoverRipples(true);
            var map=_model.Terrain;
            _tileHover=new TerrainHoverEffects(_mapRoot.transform,new TerrainHoverEffects.Map
            {
                MinX=-map.Radius,MinZ=-map.Radius,Width=map.Size,Depth=map.Size,
                // Prototype 타일은 정수 중심, Day01 호버 셀은 정수 모서리 기준이다.
                OriginOffset=new Vector3(-.5f,0,-.5f),
                Biome=(x,z)=>(TerrainBiome)(int)map.At(x,z),
                DustShader=Resources.Load<Shader>("PrototypeHoverDust")
            },_hoverDirtDust) { GrassMode=GrassHoverMode.Sway };
            _hoverHeightOffset=_tileHover.HeightOffset;
            _tileHover.WaterRippleCreated+=OnWaterRippleAudio;
        }
        private void UpdateHover()
        {
            if(_tileHover==null)return;
            var mouse=Mouse.current;
            bool paused=_modal==Modal.Menu;
            _tileHover.SetPaused(paused);
            Vector2 screen=mouse!=null?mouse.position.ReadValue():Vector2.zero;
            bool active=!paused && mouse!=null && Application.isFocused && _pointerValid
                && !OverUi(new Vector2(screen.x,Screen.height-screen.y)) && !_model.Frozen
                && !mouse.rightButton.isPressed && !mouse.middleButton.isPressed;
            if(active&&_model.Mode==InputMode.Build)
                active=_model.Territory.Contains(VillageModel.TileKey(Mathf.RoundToInt(_pointer.X),Mathf.RoundToInt(_pointer.Z)));
            bool moved=active && _hoverHadPointer && (screen-_hoverPreviousScreen).sqrMagnitude>.01f;
            float x=active&&_model.Mode==InputMode.Build?Mathf.RoundToInt(_pointer.X):_pointer.X;
            float z=active&&_model.Mode==InputMode.Build?Mathf.RoundToInt(_pointer.Z):_pointer.Z;
            Vector3? position=active ? new Vector3(x,_terrainSurface.Height(x,z),z) : (Vector3?)null;
            var biome=active ? (TerrainBiome)(int)_model.Terrain.At(_pointer) : TerrainBiome.Rocky;
            _audio?.TickEnvironment(!paused && !_model.Frozen && _camera.enabled,
                active && moved && position.HasValue && _vegetationGrass!=null && _vegetationGrass.HasGrassAt(position.Value,_grassContactVisible));
            _tileHover.SetPointer(position,biome,moved);
            _tileHover.Tick(paused?0:Time.deltaTime);
            _hoverPreviousScreen=screen;_hoverHadPointer=active;
        }
        private void OnWaterRippleAudio()=>_audio?.PlayWaterRipple();
        private void ResetHover(){_tileHover?.Reset();_hoverHadPointer=false;}
        private void DisposeHover(){_waterSurface?.SetHoverRipples(false);if(_tileHover!=null)_tileHover.WaterRippleCreated-=OnWaterRippleAudio;_tileHover?.Dispose();_tileHover=null;_hoverHeightOffset=null;_hoverHadPointer=false;}
    }
}
