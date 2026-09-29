using Afterlife.Modules.Interaction;
using System.Collections.Generic;
using Afterlife.Modules.Destruction;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private readonly Dictionary<int,PrototypeSnowTarget> _snowTargets=new Dictionary<int,PrototypeSnowTarget>();
        private void AttachSnow(Body body,GameObject root)
        {
            if(body.Unit || _model.Terrain.At(body.Position)!=TerrainKind.Snow || !_resourceSurfaces.TryGetValue(body.Id,out var surfaces))return;
            var target=root.AddComponent<PrototypeSnowTarget>();target.Initialize(_renderCache[body.Id],_settings.EnvironmentValues);
            var extra=new List<ResourceSurface>();
            foreach(var surface in surfaces)
            {
                var cap=target.AddCap(surface.Pick);
                if(cap.Renderer!=null)
                {
                    cap.Renderer.gameObject.AddComponent<PrototypeTint>().Original=Color.white;
                    extra.Add(new ResourceSurface{Pick=cap.Renderer.GetComponent<PickSurface>(),Wood=surface.Wood});
                }
            }
            surfaces.AddRange(extra);_snowTargets.Add(body.Id,target);
            // 수관 잎은 원본 렌더러만 소비하고, 눈 덮개는 플래시/제거 수명에 포함한다.
            var renderers=new List<Renderer>(_renderCache[body.Id]);foreach(var surface in extra)renderers.Add(surface.Pick.VisibleRenderer);
            _renderCache[body.Id]=renderers.ToArray();target.WriteProperties();
            if(_treeLeaves.TryGetValue(body.Id,out var tree))tree.Revision++;
        }
        private void HitSnow(ToolEffect tool)
        {
            if(!_snowTargets.TryGetValue(tool.Body,out var target))return;
            var positions=target.Hit(tool.HitPosition);
            if(_treeLeaves.TryGetValue(tool.Body,out var tree))tree.Revision++;
            if(_impactPool==null)_impactPool=new ImpactChipPool(transform,null);
            foreach(var position in positions)
            {
                if(_impactChips.Count>=128)break;
                var go=_impactPool.Rent();go.name="SnowImpactFlake";go.transform.position=position;go.transform.localScale=Vector3.one*.055f;
                go.GetComponent<Renderer>().sharedMaterial=_surface;Tint(go.GetComponent<Renderer>(),new Color(.87f,.94f,1));
                _impactChips.Add(new ImpactChip{Object=go,Velocity=Vector3.up*.5f+Random.insideUnitSphere*.7f});
            }
        }
        private void UpdateSnow()
        {
            foreach(var pair in _snowTargets)
                if(pair.Value.WriteProperties() && _treeLeaves.TryGetValue(pair.Key,out var tree))tree.Revision++;
        }
        private void RemoveSnow(int id){if(!_snowTargets.TryGetValue(id,out var target))return;target.Release();_snowTargets.Remove(id);}
        private void ResetSnow(){foreach(var target in _snowTargets.Values)if(target!=null)target.Release();_snowTargets.Clear();}
    }
}
