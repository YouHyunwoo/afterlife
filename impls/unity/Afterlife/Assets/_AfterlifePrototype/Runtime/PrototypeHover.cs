using Afterlife.Devlog.Hover;
using Afterlife.Devlog.Terrain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private ShowcaseTileHover _tileHover;
        private System.Func<Vector3,float> _hoverHeightOffset;
        private Vector2 _hoverPreviousScreen;
        private bool _hoverHadPointer;

        private void InitializeHover()
        {
            _waterSurface?.SetHoverRipples(true);
            var map=_model.Terrain;
            _tileHover=new ShowcaseTileHover(transform,new ShowcaseTileHover.Map
            {
                MinX=-map.Radius,MinZ=-map.Radius,Width=map.Size,Depth=map.Size,
                // Prototype 타일은 정수 중심, Day01 호버 셀은 정수 모서리 기준이다.
                OriginOffset=new Vector3(-.5f,0,-.5f),
                Biome=(x,z)=>(ShowcaseBiome)(int)map.At(x,z),
                DustShader=Resources.Load<Shader>("PrototypeHoverDust")
            }) { GrassMode=GrassHoverMode.Sway };
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
            bool moved=active && _hoverHadPointer && (screen-_hoverPreviousScreen).sqrMagnitude>.01f;
            Vector3? position=active ? new Vector3(_pointer.X,_terrainSurface.Height(_pointer.X,_pointer.Z),_pointer.Z) : (Vector3?)null;
            var biome=active ? (ShowcaseBiome)(int)_model.Terrain.At(_pointer) : ShowcaseBiome.Rocky;
            _audio?.TickEnvironment(!paused && !_model.Frozen && _camera.enabled,
                active && moved && position.HasValue && _vegetationGrass!=null && _vegetationGrass.HasGrassAt(position.Value));
            _tileHover.SetPointer(position,biome,moved);
            _tileHover.Tick(paused?0:Time.deltaTime);
            _hoverPreviousScreen=screen;_hoverHadPointer=active;
        }
        private void OnWaterRippleAudio()=>_audio?.PlayWaterRipple();
        private void ResetHover(){_tileHover?.Reset();_hoverHadPointer=false;}
        private void DisposeHover(){_waterSurface?.SetHoverRipples(false);if(_tileHover!=null)_tileHover.WaterRippleCreated-=OnWaterRippleAudio;_tileHover?.Dispose();_tileHover=null;_hoverHeightOffset=null;_hoverHadPointer=false;}
    }
}
