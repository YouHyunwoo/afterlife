using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Modules.Water
{
    // 불투명 해저는 지형 모듈이 소유한다. 이 모듈은 수면/외곽 수벽/재질만 소유한다.
    public sealed class WaterSurface : IDisposable
    {
        private readonly Transform _root;
        private readonly Material _material;
        private readonly List<Mesh> _meshes=new List<Mesh>();
        public WaterSurface(Transform parent,int radius,Func<int,int,bool> water,Shader shader,float height,float bottom)
        {
            if(shader==null)throw new ArgumentNullException(nameof(shader));
            _root=new GameObject("WaterSurface").transform;_root.SetParent(parent,false);
            // 기존 투명 캐릭터/효과보다 먼저 합성한다.
            _material=new Material(shader){name="CoastalWater",renderQueue=2990};
            _material.SetFloat("_BottomY",bottom);_material.SetFloat("_GridStrength",0);_material.SetFloat("_HoverRipples",0);
            for(int z=-radius;z<=radius;z+=8)for(int x=-radius;x<=radius;x+=8)
                Build(x,z,Math.Min(x+7,radius),Math.Min(z+7,radius),radius,water,height,bottom);
        }
        public void SetHoverRipples(bool enabled)=>_material.SetFloat("_HoverRipples",enabled?1:0);
        public void SetOpenSeaFoam(float amount)=>_material.SetFloat("_OpenSeaFoam",Mathf.Clamp01(amount));
        private void Build(int minX,int minZ,int maxX,int maxZ,int radius,Func<int,int,bool> water,float height,float bottom)
        {
            var data=WaterGeometry.Calculate(minX,minZ,maxX,maxZ,radius,water,height,bottom);
            var vertices=data.Vertices;var normals=data.Normals;var uv=data.Uv;var shores=data.Shores;var indices=data.Indices;
            if(vertices.Count==0)return;
            var mesh=new Mesh{name="WaterChunk_"+minX+"_"+minZ};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetUVs(1,shores);mesh.SetTriangles(indices,0);
            var go=new GameObject(mesh.name);go.transform.SetParent(_root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=_material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            _meshes.Add(mesh);
        }
        public void Dispose()
        {
            _root.gameObject.SetActive(false);Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_root.gameObject);
            foreach(var mesh in _meshes)Afterlife.Modules.UnitySupport.ObjectLifetime.Release(mesh);
            _meshes.Clear();Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_material);
        }
    }
}
