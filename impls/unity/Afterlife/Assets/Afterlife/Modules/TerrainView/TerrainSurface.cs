using System;
using System.Collections.Generic;
using Afterlife.Modules.Terrain;
using UnityEngine;
namespace Afterlife.Modules.TerrainView
{
    // Unity 적용 계층. Geometry는 GameObject 없이 에디터/런타임에서 직접 계산할 수 있다.
    public sealed class TerrainSurface : IDisposable
    {
        public const float BaseHeight=TerrainGeometry.BaseHeight;
        private readonly Transform _root;
        private readonly Material _material;
        private readonly List<Mesh> _meshes=new List<Mesh>();
        public TerrainGeometry Geometry { get; }
        public TerrainSurface(Transform parent,int radius,Func<int,int,TerrainBiome> biome,Material material,Color[] palette,CoastalTerrain coast=null)
            :this(parent,new TerrainGeometry(radius,biome,palette,coast),material) { }
        public TerrainSurface(Transform parent,TerrainGeometry geometry,Material material)
        {
            Geometry=geometry;
            _root=new GameObject("TerrainSurface").transform;_root.SetParent(parent,false);
            _material=new Material(material) { name="ContinuousBiomeTerrain",renderQueue=2000 };
            _material.SetInt("_SrcBlend",1);_material.SetInt("_DstBlend",0);_material.SetInt("_ZWrite",1);
            _material.SetFloat("_Ground",1);_material.SetFloat("_TerrainRelief",1);_material.SetFloat("_VertexPalette",1);_material.SetColor("_BaseColor",Color.white);
            foreach(var chunk in geometry.Chunks) {
                var mesh=new Mesh { name=chunk.Name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.vertices=chunk.Vertices;mesh.normals=chunk.Normals;mesh.colors=chunk.Colors;mesh.triangles=chunk.Indices;mesh.RecalculateBounds();_meshes.Add(mesh);
                var go=new GameObject(chunk.Name);go.transform.SetParent(_root,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=_material;
            }
        }
        public float Height(float x,float z)=>Geometry.Height(x,z);
        public Color SurfaceColor(float x,float z)=>Geometry.SurfaceColor(x,z);
        public bool Raycast(Ray worldRay,out Vector3 point,Func<Vector3,float> offset=null)
        {
            var local=new Ray(_root.InverseTransformPoint(worldRay.origin),_root.InverseTransformVector(worldRay.direction));
            bool hit=Geometry.Raycast(local,out var p,offset==null?null:(Func<Vector3,float>)(v=>offset(_root.TransformPoint(v))));
            point=_root.TransformPoint(p);return hit;
        }
        public void Dispose()
        {
            if(_root!=null){_root.gameObject.SetActive(false);Release(_root.gameObject);}
            foreach(var mesh in _meshes)Release(mesh);_meshes.Clear();Release(_material);
        }
        private static void Release(UnityEngine.Object value) { if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value); }
    }
}
