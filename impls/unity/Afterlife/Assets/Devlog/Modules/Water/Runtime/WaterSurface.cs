using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Devlog.Water
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
        private void Build(int minX,int minZ,int maxX,int maxZ,int radius,Func<int,int,bool> water,float height,float bottom)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
            var shores=new List<Vector4>();var indices=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Vector4 shore)
            {
                int first=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                uv.Add(new Vector2(0,0));uv.Add(new Vector2(0,1));uv.Add(new Vector2(1,0));uv.Add(new Vector2(1,1));
                for(int i=0;i<4;i++){normals.Add(normal);shores.Add(shore);}
                indices.Add(first);indices.Add(first+1);indices.Add(first+2);indices.Add(first+2);indices.Add(first+1);indices.Add(first+3);
            }
            bool Land(int x,int z)=>Math.Abs(x)<=radius && Math.Abs(z)<=radius && !water(x,z);
            for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++)
            {
                if(!water(x,z))continue;
                var a=new Vector3(x-.5f,height,z-.5f);var b=new Vector3(x-.5f,height,z+.5f);
                var c=new Vector3(x+.5f,height,z-.5f);var d=new Vector3(x+.5f,height,z+.5f);
                Quad(a,b,c,d,Vector3.up,new Vector4(Land(x-1,z)?1:0,Land(x+1,z)?1:0,Land(x,z-1)?1:0,Land(x,z+1)?1:0));
                // 내부 물 타일 경계에는 면을 만들지 않는다.
                void Wall(Vector3 left,Vector3 right,Vector3 normal)
                { Quad(left,right,new Vector3(left.x,bottom,left.z),new Vector3(right.x,bottom,right.z),normal,Vector4.zero); }
                if(x==-radius)Wall(b,a,Vector3.left);
                if(x==radius)Wall(c,d,Vector3.right);
                if(z==-radius)Wall(a,c,Vector3.back);
                if(z==radius)Wall(d,b,Vector3.forward);
            }
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
            _root.gameObject.SetActive(false);UnityEngine.Object.Destroy(_root.gameObject);
            foreach(var mesh in _meshes)UnityEngine.Object.Destroy(mesh);
            _meshes.Clear();UnityEngine.Object.Destroy(_material);
        }
    }
}
