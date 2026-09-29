using System.Collections.Generic;
using Afterlife.Modules.Destruction;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private sealed class ImpactChip { public GameObject Object; public Vector3 Velocity; public float Age; }
        private readonly List<ImpactChip> _impactChips=new List<ImpactChip>();
        private ImpactChipPool _impactPool;
        private Vector3 ChestClickAway(Body body)
        {
            Vector3 center=GroundPosition(body.Position,.3f);
            if(_renderCache.TryGetValue(body.Id,out var renderers))
            {
                Bounds bounds=default;bool found=false;
                foreach(var renderer in renderers)
                {
                    if(renderer==null || !renderer.enabled)continue;
                    var filter=renderer.GetComponent<MeshFilter>();
                    if(filter==null || filter.sharedMesh==_contactMesh)continue;
                    if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
                }
                if(found)center=bounds.center;
            }
            var mouse=UnityEngine.InputSystem.Mouse.current;
            if(mouse!=null)
            {
                Vector2 screen=mouse.position.ReadValue();
                var ray=_camera.ViewportPointToRay(new Vector3(screen.x/Screen.width,screen.y/Screen.height,0));
                // 클릭 표면의 높이가 아니라 보이는 상자 중심 높이에 투영해야 화면상의 반대쪽이 된다.
                if(new Plane(Vector3.up,center).Raycast(ray,out float distance))
                {
                    Vector3 away=center-ray.GetPoint(distance);away.y=0;
                    if(away.sqrMagnitude>.0001f)return away.normalized;
                }
            }
            return Vector3.ProjectOnPlane(_camera.transform.forward,Vector3.up).normalized;
        }
        private void EmitImpactChips(ToolEffect tool)
        {
            bool resource=tool.Kind==Kind.Tree||tool.Kind==Kind.Rock;
            if(resource && tool.HitSurface==null)return;
            bool support=VillageModel.IsBuildingKind(tool.Kind);
            bool hand=tool.Kind==Kind.Chest||tool.Kind==Kind.Treasure;
            Color color=resource?(tool.Kind==Kind.Tree?new Color(.57f,.34f,.13f):new Color(.55f,.60f,.62f)):
                support?new Color(.38f,.9f,.65f):hand?new Color(1,.8f,.25f):new Color(1,.88f,.6f);
            if(_impactPool==null)_impactPool=new ImpactChipPool(transform,null);
            for(int i=0;i<5 && _impactChips.Count<128;i++)
            {
                var go=_impactPool.Rent();go.name="InteractionImpactChip";
                go.transform.position=tool.HitPosition;go.transform.localScale=Vector3.one*.055f;
                var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=_surface;
                Tint(renderer,color);
                _impactChips.Add(new ImpactChip{Object=go,Velocity=tool.HitNormal*Random.Range(.45f,1.1f)+Vector3.up*Random.Range(.45f,1.3f)+Random.insideUnitSphere*.35f});
            }
        }
        private void UpdateImpactChips()
        {
            float dt=_modal==Modal.Menu?0:Time.deltaTime;
            for(int i=_impactChips.Count-1;i>=0;i--)
            {
                var chip=_impactChips[i];chip.Age+=dt;
                if(chip.Age>=.65f){_impactPool.Return(chip.Object);_impactChips.RemoveAt(i);continue;}
                chip.Velocity+=Vector3.down*9*dt;chip.Object.transform.position+=chip.Velocity*dt;
                chip.Object.transform.Rotate(new Vector3(110,160,70)*dt);
                chip.Object.transform.localScale=Vector3.one*.055f*Mathf.Clamp01((.65f-chip.Age)/.2f);
            }
        }
        private void ResourceFlash(int id,float flash)
        {
            if(!_renderCache.TryGetValue(id,out var renderers))return;
            foreach(var renderer in renderers)
            {
                renderer.GetPropertyBlock(_block);
                if(Mathf.Approximately(_block.GetFloat("_Flash"),flash))continue;
                _block.SetFloat("_Flash",flash);renderer.SetPropertyBlock(_block);
                if(_leafTints.TryGetValue(renderer,out var tint))tint.Tree.Revision++;
            }
        }
        private void ResetImpactChips()
        {
            foreach(var chip in _impactChips)_impactPool.Return(chip.Object);
            _impactChips.Clear();
        }
    }
}
