using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afterlife.Devlog.Day01
{
    // 타깃/입력에 의존하지 않는 표면 카드 생성기. 원본 형상은 보존한다.
    public sealed class ShowcaseLeafCards : IDisposable
    {
        public struct Surface { public Transform Transform; public Vector3[] Vertices; public Vector3[] Normals; public int[] Triangles; public Color[] Colors; public Renderer Renderer; }
        private readonly List<GameObject> _objects=new List<GameObject>();
        private readonly List<Mesh> _meshes=new List<Mesh>();
        private readonly List<Renderer> _sources=new List<Renderer>();
        private readonly Material _material;
        private readonly MaterialPropertyBlock _properties=new MaterialPropertyBlock();
        private readonly List<Renderer> _renderers=new List<Renderer>();
        public bool Enabled { get; private set; }=true;
        public int Count { get; private set; }
        public bool RotationVariation { get; private set; }=true;
        public void ToggleRotation()
        {
            RotationVariation=!RotationVariation;
            if (_material!=null) _material.SetFloat("_LeafRotationVariation",RotationVariation ? 1 : 0);
        }
        public ShowcaseLeafCards(IEnumerable<Surface> surfaces)
        {
            var shader=Shader.Find("Afterlife/Devlog/LeafCards");
            if (shader==null) { Debug.LogError("Leaf card shader missing."); return; }
            _material=new Material(shader);
            _material.SetFloat("_LeafRotationVariation",1);
            _material.SetTexture("_Leaf",Resources.Load<Texture2D>("GrassSprites/Grass2"));
            var random=new System.Random(91823);
            var occupiedAnchors=new List<Vector3>();
            var sourceList=new List<Surface>(surfaces);
            double totalArea=0;
            // 상한을 적용하기 전에 모든 수관 면의 넓이를 합산한다.
            foreach (var source in sourceList)
            {
                if (source.Colors==null || source.Colors.Length!=source.Vertices.Length) continue;
                for (int t=0;t<source.Triangles.Length;t+=3)
                {
                    int a=source.Triangles[t],b=source.Triangles[t+1],c=source.Triangles[t+2];
                    Color tint=(source.Colors[a]+source.Colors[b]+source.Colors[c])/3;
                    if (tint.g<tint.r*1.08f || tint.g<tint.b*1.05f) continue;
                    totalArea+=Vector3.Cross(source.Transform.TransformVector(source.Vertices[b]-source.Vertices[a]),
                        source.Transform.TransformVector(source.Vertices[c]-source.Vertices[a])).magnitude*.5f;
                }
            }
            int budget=Math.Min(2000,(int)Math.Round(totalArea*200));
            double cumulativeArea=0;
            int allocated=0;
            foreach (var source in sourceList)
            {
                var positions=new List<Vector3>(); var normals=new List<Vector3>(); var uv=new List<Vector2>();
                var sizes=new List<Vector4>(); var colors=new List<Color>(); var indices=new List<int>();
                if (source.Colors==null || source.Colors.Length!=source.Vertices.Length) continue;
                for (int t=0;t<source.Triangles.Length;t+=3)
                {
                    int a=source.Triangles[t],b=source.Triangles[t+1],c=source.Triangles[t+2];
                    Color tint=(source.Colors[a]+source.Colors[b]+source.Colors[c])/3;
                    // 공유 FBX의 초록색 수관만 사용하며 갈색 줄기는 제외한다.
                    if (tint.g<tint.r*1.08f || tint.g<tint.b*1.05f) continue;
                    Vector3 va=source.Vertices[a],vb=source.Vertices[b],vc=source.Vertices[c];
                    float area=Vector3.Cross(source.Transform.TransformVector(vb-va),source.Transform.TransformVector(vc-va)).magnitude*.5f;
                    cumulativeArea+=area;
                    int throughHere=totalArea>0 ? Math.Min(budget,(int)Math.Round(cumulativeArea/totalArea*budget)) : 0;
                    int count=throughHere-allocated;
                    allocated=throughHere;
                    Vector3 normal=Vector3.Cross(vb-va,vc-va).normalized;
                    // 임포트된 표면 법선으로 면 방향을 검증한다. 아랫면을 윗면으로 오인하지 않는다.
                    if (source.Normals!=null && source.Normals.Length==source.Vertices.Length)
                    {
                        Vector3 imported=source.Normals[a]+source.Normals[b]+source.Normals[c];
                        if (Vector3.Dot(normal,imported)<0) normal=-normal;
                    }
                    for (int j=0;j<count;j++)
                    {
                        float u=Mathf.Sqrt((float)random.NextDouble()),v=(float)random.NextDouble();
                        Vector3 anchor=va*(1-u)+vb*(u*(1-v))+vc*(u*v);
                        // 간격을 벌리지 않고 사실상 동일한 부착 위치만 제외한다(월드 0.001).
                        Vector3 worldAnchor=source.Transform.TransformPoint(anchor);
                        bool duplicate=false;
                        foreach (var occupied in occupiedAnchors)
                            if ((occupied-worldAnchor).sqrMagnitude<.000001f) { duplicate=true; break; }
                        if (duplicate) continue;
                        occupiedAnchors.Add(worldAnchor);
                        Color color=source.Colors[a]*(1-u)+source.Colors[b]*(u*(1-v))+source.Colors[c]*(u*v);
                        float size=.12f+(float)random.NextDouble()*.10f,phase=(float)random.NextDouble()*6.283f;
                        // 윗면은 눈을 완전히 덮고, 하향면만 원래 잎 색을 유지한다.
                        float snow=source.Transform.worldToLocalMatrix.transpose.MultiplyVector(normal).normalized.y>.05f ? 1 : 0;
                        int start=positions.Count;
                        for (int corner=0;corner<4;corner++)
                        {
                            positions.Add(anchor); normals.Add(normal); colors.Add(color);
                            uv.Add(new Vector2(corner==1 || corner==2 ? 1 : 0,corner>=2 ? 1 : 0));
                            sizes.Add(new Vector4(size,size*.8f,phase,snow));
                        }
                        indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3}); Count++;
                    }
                }
                if (positions.Count==0) continue;
                var mesh=new Mesh { name="SurfaceLeafCards" };
                mesh.SetVertices(positions); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetUVs(0,uv); mesh.SetUVs(1,sizes); mesh.SetTriangles(indices,0);
                var bounds=mesh.bounds; bounds.Expand(1); mesh.bounds=bounds;
                var go=new GameObject("SurfaceLeafCards"); go.transform.SetParent(source.Transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=_material; renderer.shadowCastingMode=ShadowCastingMode.Off;
                _objects.Add(go); _meshes.Add(mesh); _sources.Add(source.Renderer); _renderers.Add(renderer);
            }
        }
        public void Toggle() { Enabled=!Enabled; Tick(); }
        public void Tick()
        {
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
                _renderers[i].SetPropertyBlock(_properties);
            }
        }
        public void Dispose()
        {
            foreach (var go in _objects) UnityEngine.Object.Destroy(go);
            foreach (var mesh in _meshes) UnityEngine.Object.Destroy(mesh);
            if (_material!=null) UnityEngine.Object.Destroy(_material);
        }
    }
}
