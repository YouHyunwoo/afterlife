using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Afterlife.Devlog.Contracts;

namespace Afterlife.Devlog.Day01
{
    // 원본은 보존하고 연결된 윗면/밑면/테두리를 가진 별도 눈 덩어리를 만든다.
    public sealed class ShowcaseSnowCap : IDisposable
    {
        public readonly Renderer Renderer;
        private readonly ISnowTarget _target;
        private readonly Transform _transform;
        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly ShowcasePickMesh _pick;
        private readonly Vector3[] _base, _offset, _vertices;
        private readonly int _topCount;
        public struct Sample
        {
            public Vector3 Position, MaskPosition;
            public float Before, Weight;
        }

        public ShowcaseSnowCap(ShowcasePickSurface source, ISnowTarget target,bool pine)
        {
            _target = target;
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();
            var welded = new Dictionary<Vector3Int,int>();
            int Add(Vector3 position, Vector3 normal, Color color)
            {
                var key = new Vector3Int(Mathf.RoundToInt(position.x*100000),Mathf.RoundToInt(position.y*100000),Mathf.RoundToInt(position.z*100000));
                if (welded.TryGetValue(key,out int old)) { normals[old] += normal; return old; }
                int index = positions.Count; welded.Add(key,index);
                positions.Add(position); normals.Add(normal); colors.Add(color); return index;
            }
            void Triangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
            {
                Vector3 normal = Vector3.Cross(b-a,c-a).normalized;
                if (normal.sqrMagnitude < .5f) return;
                indices.Add(Add(a,normal,ca)); indices.Add(Add(b,normal,cb)); indices.Add(Add(c,normal,cc));
            }
            var data = source.Mesh;
            if (data == null || source.VisibleRenderer == null) return;
            Color Palette(int index) => data.Colors != null && data.Colors.Length == data.Vertices.Length ? data.Colors[index] : Color.white;
            bool Leaf(Color color) => color.g >= color.r*1.1f && color.g >= color.b*1.05f;
            for (int i=0;i<data.Triangles.Length;i+=3)
            {
                int ia=data.Triangles[i], ib=data.Triangles[i+1], ic=data.Triangles[i+2];
                Vector3 a=data.Vertices[ia], b=data.Vertices[ib], c=data.Vertices[ic];
                Color ca=Palette(ia), cb=Palette(ib), cc=Palette(ic);
                Vector3 faceNormal=Vector3.Cross(b-a,c-a);
                if (data.Normals!=null && data.Normals.Length==data.Vertices.Length
                    && Vector3.Dot(faceNormal,data.Normals[ia]+data.Normals[ib]+data.Normals[ic])<0) faceNormal=-faceNormal;
                Vector3 worldNormal=source.transform.worldToLocalMatrix.transpose.MultiplyVector(faceNormal).normalized;
                if (worldNormal.y < (pine && Leaf(ca) && Leaf(cb) && Leaf(cc) ? .05f : .25f)) continue;
                // 타격 부위의 눈이 국소적으로 내려앉도록 각 원본 면을 네 개로 나눈다.
                Vector3 ab=(a+b)*.5f, bc=(b+c)*.5f, ac=(a+c)*.5f;
                Color cab=(ca+cb)*.5f, cbc=(cb+cc)*.5f, cac=(ca+cc)*.5f;
                Triangle(a,ab,ac,ca,cab,cac); Triangle(ab,b,bc,cab,cb,cbc);
                Triangle(ac,bc,c,cac,cbc,cc); Triangle(ab,bc,ac,cab,cbc,cac);
            }
            if (positions.Count == 0) return;
            var go = new GameObject("SnowCap");
            go.transform.SetParent(source.transform,false); _transform = go.transform;
            int count=positions.Count;
            _base=positions.ToArray(); _offset=new Vector3[count]; _vertices=new Vector3[count*2];
            var allColors = new Color[count*2];
            var normalMatrix=source.transform.worldToLocalMatrix.transpose;
            for (int i=0;i<count;i++)
            {
                Vector3 normal=normalMatrix.MultiplyVector(normals[i]).normalized;
                _offset[i]=source.transform.InverseTransformVector(normal*.085f);
                allColors[i]=allColors[i+count]=colors[i];
            }
            var edges=new Dictionary<(int,int),(int a,int b,int count)>();
            void Edge(int a,int b)
            {
                var key=(Mathf.Min(a,b),Mathf.Max(a,b));
                edges.TryGetValue(key,out var edge);
                edges[key]=(a,b,edge.count+1);
            }
            int topCount=indices.Count;
            _topCount=topCount;
            for (int i=0;i<topCount;i+=3)
            {
                int a=indices[i],b=indices[i+1],c=indices[i+2];
                Edge(a,b); Edge(b,c); Edge(c,a);
                indices.Add(c+count); indices.Add(b+count); indices.Add(a+count);
            }
            foreach (var edge in edges.Values)
                if (edge.count==1)
                {
                    int a=edge.a,b=edge.b;
                    indices.Add(b); indices.Add(a); indices.Add(a+count);
                    indices.Add(b); indices.Add(a+count); indices.Add(b+count);
                }
            _mesh=new Mesh { name="Connected Snow Cap", indexFormat=IndexFormat.UInt32 };
            _mesh.MarkDynamic(); _mesh.vertices=_vertices; _mesh.triangles=indices.ToArray(); _mesh.colors=allColors;
            go.AddComponent<MeshFilter>().sharedMesh=_mesh;
            Renderer=go.AddComponent<MeshRenderer>();
            _material=new Material(source.VisibleRenderer.sharedMaterial) { name="Devlog Snow Cap" };
            _material.SetFloat("_SnowShell",1); _material.SetFloat("_Snow",1); _material.SetFloat("_SnowPine",1);
            Renderer.sharedMaterial=_material;
            _pick=ScriptableObject.CreateInstance<ShowcasePickMesh>();
            _pick.Vertices=_vertices; _pick.Triangles=_mesh.triangles; _pick.Colors=allColors;
            var surface=go.AddComponent<ShowcasePickSurface>();
            surface.Mesh=_pick; surface.VisibleRenderer=Renderer; surface.Target=target; surface.SnowCap=true;
            Refresh();
        }

        public void CollectSamples(Vector3 hit,List<Sample> samples)
        {
            if (_mesh==null || !Renderer.enabled) return;
            Vector3 localHit=_target.Visual.InverseTransformPoint(hit);
            int[] triangles=_pick.Triangles;
            float wind=_material.HasProperty("_Wind") ? _material.GetFloat("_Wind") : 0;
            for (int i=0;i<_topCount;i+=3)
            {
                Vector3 a=_transform.TransformPoint(_vertices[triangles[i]]);
                Vector3 b=_transform.TransformPoint(_vertices[triangles[i+1]]);
                Vector3 c=_transform.TransformPoint(_vertices[triangles[i+2]]);
                float area=Vector3.Cross(b-a,c-a).magnitude*.5f;
                if (area<.000001f) continue;
                for (int sample=0;sample<12;sample++)
                {
                    // 균일 면적의 무게중심 표본. 실제 눈 윗면에서만 방출 후보를 만든다.
                    float u=Mathf.Sqrt(Mathf.Repeat((sample+1)*.618034f+i*.137f,1));
                    float v=Mathf.Repeat((sample+1)*.414214f+i*.071f,1);
                    Vector3 point=a*(1-u)+b*(u*(1-v))+c*(u*v);
                    if ((_target.Visual.InverseTransformPoint(point)-localHit).sqrMagnitude>.7f*.7f) continue;
                    float before=_target.SnowRemaining(point);
                    if (!_target.SnowVisible(point)) continue;
                    Vector3 rendered=point;
                    rendered.x+=Mathf.Sin(Time.time*1.6f+point.z*1.7f+point.x)*.025f*wind*Mathf.Clamp01(point.y*2);
                    samples.Add(new Sample { Position=rendered, MaskPosition=point, Before=before, Weight=area/12 });
                }
            }
        }

        public void Refresh()
        {
            if (_mesh == null) return;
            Renderer.enabled=_target.HasSnow;
            for (int i=0;i<_base.Length;i++)
            {
                float remaining=_target.SnowRemaining(_transform.TransformPoint(_base[i]));
                _vertices[i]=_base[i]+_offset[i]*remaining;
                // 밑면은 원본 안에 살짝 묻어 접촉부의 z-fighting을 피한다.
                _vertices[i+_base.Length]=_base[i]-_offset[i]*.04f;
            }
            _mesh.vertices=_vertices; _mesh.RecalculateNormals(); _mesh.RecalculateBounds();
            _pick.Vertices=_vertices; _pick.Bounds=_mesh.bounds;
        }

        public void Dispose()
        {
            if (_transform != null) UnityEngine.Object.Destroy(_transform.gameObject);
            if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
            if (_material != null) UnityEngine.Object.Destroy(_material);
            if (_pick != null) UnityEngine.Object.Destroy(_pick);
        }
    }
}
