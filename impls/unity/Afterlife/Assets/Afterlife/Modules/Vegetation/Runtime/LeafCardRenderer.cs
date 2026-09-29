using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afterlife.Modules.Vegetation
{
    // 타깃/입력에 의존하지 않는 표면 카드 생성기. 원본 형상은 보존한다.
    public sealed class LeafCardRenderer : IDisposable
    {
        public struct Surface { public Transform Transform; public Vector3[] Vertices; public Vector3[] Normals; public int[] Triangles; public Color[] Colors; public Renderer Renderer; }
        private readonly List<GameObject> _objects=new List<GameObject>();
        private readonly List<Mesh> _meshes=new List<Mesh>();
        private readonly List<Renderer> _sources=new List<Renderer>();
        private readonly Material _material;
        private readonly MaterialPropertyBlock _properties=new MaterialPropertyBlock();
        private readonly List<Renderer> _renderers=new List<Renderer>();
        private readonly Func<int> _version;
        private readonly Action<Renderer,MaterialPropertyBlock> _adaptProperties;
        private int _lastVersion=int.MinValue;
        private readonly List<VegetationDensityController> _densities=new List<VegetationDensityController>();
        public float FullDensityDistance { get; set; }=30;
        public float HalfDensityDistance { get; set; }=45;
        public bool Enabled { get; private set; }=true;
        public int Count { get; private set; }
        public bool RotationVariation { get; private set; }=true;
        public void ToggleRotation()
        {
            RotationVariation=!RotationVariation;
            if (_material!=null) _material.SetFloat("_LeafRotationVariation",RotationVariation ? 1 : 0);
        }
        public LeafCardRenderer(IEnumerable<Surface> surfaces,Func<int> version=null,Shader shader=null,int maxCards=2000,Action<Renderer,MaterialPropertyBlock> adaptProperties=null)
        {
            _version=version;
            _adaptProperties=adaptProperties;
            if(shader==null)shader=Shader.Find("Afterlife/Vegetation/LeafCards");
            if (shader==null) { Debug.LogError("Leaf card shader missing."); return; }
            _material=new Material(shader);
            _material.SetFloat("_LeafRotationVariation",1);
            _material.SetTexture("_Leaf",Resources.Load<Texture2D>("GrassSprites/Grass2"));
            var sourceList=new List<Surface>(surfaces);var input=new List<LeafSurfaceData>();
            foreach(var source in sourceList)input.Add(new LeafSurfaceData { LocalToWorld=source.Transform.localToWorldMatrix,Vertices=source.Vertices,Normals=source.Normals,Triangles=source.Triangles,Colors=source.Colors });
            var geometry=LeafGeometry.Calculate(input,maxCards);Count=geometry.Count;
            foreach(var data in geometry.Meshes) {
                var source=sourceList[data.Source];var mesh=new Mesh { name="SurfaceLeafCards" };
                VegetationDensityController.ShuffleCards(data.Indices,91823);
                mesh.SetVertices(data.Positions);mesh.SetNormals(data.Normals);mesh.SetColors(data.Colors);mesh.SetUVs(0,data.Uv);mesh.SetUVs(1,data.Sizes);mesh.SetTriangles(data.Indices,0);
                var bounds=mesh.bounds;bounds.Expand(1);mesh.bounds=bounds;
                var go=new GameObject("SurfaceLeafCards");go.transform.SetParent(source.Transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=_material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                _objects.Add(go);_meshes.Add(mesh);_sources.Add(source.Renderer);_renderers.Add(renderer);_densities.Add(new VegetationDensityController(mesh,renderer,data.Indices.Count));
            }
        }
        public void Toggle() { Enabled=!Enabled; _lastVersion=int.MinValue; Tick(); }
        public void Tick(Camera camera=null)
        {
            if (camera!=null) foreach (var density in _densities) density.Tick(camera,FullDensityDistance,HalfDensityDistance);
            int version=_version!=null ? _version() : 0;
            if (_version!=null && version==_lastVersion) return;
            _lastVersion=version;
            for (int i=0;i<_objects.Count;i++)
            {
                _objects[i].SetActive(Enabled && _sources[i]!=null && _sources[i].enabled);
                if (_sources[i]==null) continue;
                _sources[i].GetPropertyBlock(_properties);
                // 진행도/초기화는 원본 블록을 그대로 따른다. 스케일은 원본 재질 기본값도 상속한다.
                if (!_properties.HasFloat("_DissolveScale"))
                {
                    var sourceMaterial=_sources[i].sharedMaterial;
                    _properties.SetFloat("_DissolveScale",sourceMaterial!=null && sourceMaterial.HasProperty("_DissolveScale")
                        ? sourceMaterial.GetFloat("_DissolveScale") : 3.5f);
                }
                _adaptProperties?.Invoke(_sources[i],_properties);
                _renderers[i].SetPropertyBlock(_properties);
            }
        }
        public void Dispose()
        {
            foreach (var go in _objects) if(go!=null) {
                go.SetActive(false);go.transform.SetParent(null,true);
                Afterlife.Modules.UnitySupport.ObjectLifetime.Release(go);
            }
            foreach (var mesh in _meshes) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(mesh);
            if (_material!=null) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_material);
        }
    }

    // 고정 셔플의 접두 구간을 그려 밀도를 바꾼다. 거리 전환 때 정점/인덱스 배열을 재생성하지 않는다.
    public sealed class VegetationDensityController
    {
        private readonly Mesh _mesh;
        private readonly Renderer _renderer;
        private readonly int _indexCount;
        private int _level;
        public VegetationDensityController(Mesh mesh,Renderer renderer,int indexCount)
        { _mesh=mesh; _renderer=renderer; _indexCount=indexCount; }
        public static void ShuffleCards(List<int> indices,int seed)
        {
            var random=new System.Random(seed);
            for (int card=indices.Count/6-1;card>0;card--)
            {
                int other=random.Next(card+1);
                for (int k=0;k<6;k++)
                { int value=indices[card*6+k]; indices[card*6+k]=indices[other*6+k]; indices[other*6+k]=value; }
            }
        }
        public void Tick(Camera camera,float fullDistance,float halfDistance)
        {
            if (_renderer==null || !_renderer.gameObject.activeInHierarchy) return;
            float distance=Vector3.Distance(camera.transform.position,_renderer.bounds.center);
            // 경계에서 카메라가 흔들려도 밀도가 매 프레임 전환되지 않게 한다.
            int next=distance>halfDistance+(_level<2 ? 1 : -1) ? 2
                : distance>fullDistance+(_level==0 ? 1 : -1) ? 1 : 0;
            if (_level==next) return;
            _level=next;
            int count=(_indexCount/6 >> next)*6;
            var submesh=new SubMeshDescriptor(0,count,MeshTopology.Triangles)
            { bounds=_mesh.bounds,firstVertex=0,vertexCount=_mesh.vertexCount };
            _mesh.SetSubMesh(0,submesh,MeshUpdateFlags.DontRecalculateBounds);
        }
    }
}
