using System;
using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Modules.Vegetation
{
    public struct LeafSurfaceData { public Matrix4x4 LocalToWorld;public Vector3[] Vertices,Normals;public int[] Triangles;public Color[] Colors; }
    public sealed class LeafMeshData { public int Source;public List<Vector3> Positions,Normals;public List<Color> Colors;public List<Vector2> Uv;public List<Vector4> Sizes;public List<int> Indices; }
    public sealed class LeafGeometryData { public int Count;public readonly List<LeafMeshData> Meshes=new List<LeafMeshData>(); }
    public static class LeafGeometry
    {
        public static LeafGeometryData Calculate(IEnumerable<LeafSurfaceData> surfaces,int maxCards=2000)
        {
            var result=new LeafGeometryData();
            var random=new System.Random(91823);
            var occupiedAnchors=new List<Vector3>();
            var sourceList=new List<LeafSurfaceData>(surfaces);
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
                    totalArea+=Vector3.Cross(source.LocalToWorld.MultiplyVector(source.Vertices[b]-source.Vertices[a]),
                        source.LocalToWorld.MultiplyVector(source.Vertices[c]-source.Vertices[a])).magnitude*.5f;
                }
            }
            int budget=Math.Min(Math.Max(0,maxCards),(int)Math.Round(totalArea*200));
            double cumulativeArea=0;
            int allocated=0;
            for(int sourceIndex=0;sourceIndex<sourceList.Count;sourceIndex++)
            {
                var source=sourceList[sourceIndex];
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
                    float area=Vector3.Cross(source.LocalToWorld.MultiplyVector(vb-va),source.LocalToWorld.MultiplyVector(vc-va)).magnitude*.5f;
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
                        Vector3 worldAnchor=source.LocalToWorld.MultiplyPoint3x4(anchor);
                        bool duplicate=false;
                        foreach (var occupied in occupiedAnchors)
                            if ((occupied-worldAnchor).sqrMagnitude<.000001f) { duplicate=true; break; }
                        if (duplicate) continue;
                        occupiedAnchors.Add(worldAnchor);
                        Color color=source.Colors[a]*(1-u)+source.Colors[b]*(u*(1-v))+source.Colors[c]*(u*v);
                        float size=.12f+(float)random.NextDouble()*.10f,phase=(float)random.NextDouble()*6.283f;
                        // 윗면은 눈을 완전히 덮고, 하향면만 원래 잎 색을 유지한다.
                        float snow=source.LocalToWorld.inverse.transpose.MultiplyVector(normal).normalized.y>.05f ? 1 : 0;
                        int start=positions.Count;
                        for (int corner=0;corner<4;corner++)
                        {
                            positions.Add(anchor); normals.Add(normal); colors.Add(color);
                            uv.Add(new Vector2(corner==1 || corner==2 ? 1 : 0,corner>=2 ? 1 : 0));
                            sizes.Add(new Vector4(size,size*.8f,phase,snow));
                        }
                        indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3}); result.Count++;
                    }
                }
                if (positions.Count==0) continue;
                result.Meshes.Add(new LeafMeshData { Source=sourceIndex,Positions=positions,Normals=normals,Colors=colors,Uv=uv,Sizes=sizes,Indices=indices });
            }
            return result;
        }
    }
}
