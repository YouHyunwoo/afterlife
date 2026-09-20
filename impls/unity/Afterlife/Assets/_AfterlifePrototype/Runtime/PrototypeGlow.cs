using Afterlife.Devlog.Day01;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private ShowcaseCrystalGlow _glow;
        private void InitializeGlow()
        {
            // 카메라/Volume 수명만 공용 모듈이 소유하고 동적 오브젝트의 MPB는 뷰가 관리한다.
            _glow=new ShowcaseCrystalGlow(transform,_camera,System.Array.Empty<Renderer>());
        }
        private void UpdateGlow(Body body)
        {
            if(body.Kind!=Kind.House && body.Kind!=Kind.Ruins)return;
            if(!_renderCache.TryGetValue(body.Id,out var renderers))return;
            foreach(var renderer in renderers)
            {
                var surface=renderer.GetComponent<ShowcasePickSurface>();
                if(surface==null || surface.SnowCap)continue;
                float crystal=body.Kind==Kind.Ruins?3.5f:0;
                float window=body.Kind==Kind.House && body.Complete && !body.MovingOut?1:0;
                renderer.GetPropertyBlock(_block);
                if(_block.GetFloat("_CrystalGlow")==crystal && _block.GetFloat("_HouseWindows")==window)continue;
                _block.SetFloat("_CrystalGlow",crystal);_block.SetFloat("_HouseWindows",window);
                renderer.SetPropertyBlock(_block);
            }
        }
        private void DisposeGlow(){_glow?.Dispose();_glow=null;}
    }
}
