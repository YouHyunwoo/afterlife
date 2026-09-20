using System.Collections.Generic;
using Afterlife.Devlog.Day01;
using Afterlife.Devlog.DamageMarks;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private sealed class ResourceSurface
        {
            public ShowcasePickSurface Pick;
            public ShowcaseDamageMarks Marks=new ShowcaseDamageMarks();
            public bool Wood,Marked,Dirty;
            public Matrix4x4 LastMatrix;
        }
        private readonly Dictionary<int,List<ResourceSurface>> _resourceSurfaces=new Dictionary<int,List<ResourceSurface>>();
        private void AttachResourceSurfaces(Body body,Renderer[] renderers)
        {
            var surfaces=new List<ResourceSurface>();
            foreach(var renderer in renderers)
            {
                var filter=renderer.GetComponent<MeshFilter>();
                if(filter==null || filter.sharedMesh==null || filter.sharedMesh==_contactMesh || !filter.sharedMesh.isReadable)continue;
                var pick=renderer.gameObject.AddComponent<ShowcasePickSurface>();
                pick.VisibleRenderer=renderer;
                surfaces.Add(new ResourceSurface{Pick=pick,Wood=body.Kind==Kind.Tree});
            }
            if(surfaces.Count>0)_resourceSurfaces[body.Id]=surfaces;
        }
        private bool TryResourceSurface(int id,Ray ray,out ShowcasePickSurface surface,out float distance)
        {
            surface=null;distance=float.PositiveInfinity;
            if(!_resourceSurfaces.TryGetValue(id,out var surfaces))return false;
            foreach(var entry in surfaces)
                if(entry.Pick!=null && entry.Pick.Intersect(ray,out float hit,out _,false) && hit<distance)
                {surface=entry.Pick;distance=hit;}
            return surface!=null;
        }
        private void AddResourceMark(ToolEffect tool)
        {
            if(tool.Kind!=Kind.Tree && tool.Kind!=Kind.Rock)return;
            if(tool.HitSurface==null || !_resourceSurfaces.TryGetValue(tool.Body,out var surfaces))return;
            foreach(var entry in surfaces)if(entry.Pick==tool.HitSurface)
            {
                entry.Marks.Add(entry.Pick.transform,tool.HitPosition,tool.HitNormal);
                entry.Marked=entry.Dirty=true;
                break;
            }
        }
        private void UpdateResourceMarks()
        {
            foreach(var surfaces in _resourceSurfaces.Values)foreach(var entry in surfaces)
            {
                var renderer=entry.Pick!=null?entry.Pick.VisibleRenderer:null;
                if(renderer==null || !entry.Marked)continue;
                var matrix=entry.Pick.transform.worldToLocalMatrix;
                if(!entry.Dirty && matrix==entry.LastMatrix)continue;
                renderer.GetPropertyBlock(_block);
                entry.Marks.Write(_block,entry.Pick.transform,true,entry.Wood);
                renderer.SetPropertyBlock(_block);
                entry.LastMatrix=matrix;entry.Dirty=false;
            }
        }
        private void ResetResourceSurfaces()=>_resourceSurfaces.Clear();
    }
}
