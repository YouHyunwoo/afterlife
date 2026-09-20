using System.Collections.Generic;
using Afterlife.Devlog.Terrain;
using UnityEngine;

namespace Afterlife.Devlog.Shoreline
{
    public enum ShorelineMode { Square, Bevel, Rounded }

    public static class ShorelineGeometry
    {
        public const float Width=.22f;
        public static float Offset(Vector3 stagePoint,ShorelineMode mode)
        {
            if (mode==ShorelineMode.Square) return 0;
            float distance=Width;
            int cx=Mathf.FloorToInt(stagePoint.x),cz=Mathf.FloorToInt(stagePoint.z);
            // 물 타일 영역까지의 거리. 모서리와 인접 타일도 같은 좌표장을 쓴다.
            for (int z=cz-1;z<=cz+1;z++)
            for (int x=cx-1;x<=cx+1;x++)
            {
                if (x< -6 || x>=6 || z< -5 || z>=5 || ShowcaseBiomeMap.At(x,z)!=ShowcaseBiome.Water) continue;
                float dx=Mathf.Max(x-stagePoint.x,0,stagePoint.x-(x+1));
                float dz=Mathf.Max(z-stagePoint.z,0,stagePoint.z-(z+1));
                distance=Mathf.Min(distance,Mathf.Sqrt(dx*dx+dz*dz));
            }
            if (mode==ShorelineMode.Bevel) return distance-Width;
            float horizontal=Width-distance;
            return Mathf.Sqrt(Mathf.Max(0,Width*Width-horizontal*horizontal))-Width;
        }

        public static Mesh Build(Mesh source,Matrix4x4 toStage,ShorelineMode mode,int cuts=8)
        {
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var colors=new List<Color>();
            var input=source.vertices; var inputColors=source.colors;
            var slots=new List<int>[Mathf.Max(2,source.subMeshCount)];
            for (int i=0;i<slots.Length;i++) slots[i]=new List<int>();
            Matrix4x4 fromStage=toStage.inverse;
            Vector3 Move(Vector3 p)
            {
                Vector3 stage=toStage.MultiplyPoint3x4(p);
                // 블록 바닥은 고정하고 윗모서리를 낮춰 해안만 깎는다.
                stage.y+=Offset(stage,mode)*Mathf.Clamp01((stage.y+.8f)/.8f);
                return fromStage.MultiplyPoint3x4(stage);
            }
            cuts=Mathf.Clamp(cuts,1,8);
            // 윗면만 가공한다. 옆면은 이 윗면의 실제 경계로부터 재생성한다.
            for (int slot=0;slot<1;slot++)
            {
                slots[slot]=new List<int>(); var triangles=source.GetTriangles(slot);
                for (int t=0;t<triangles.Length;t+=3)
                {
                    int ia=triangles[t],ib=triangles[t+1],ic=triangles[t+2];
                    Vector3 Point(int u,int v) => input[ia]+(input[ib]-input[ia])*(u/(float)cuts)+(input[ic]-input[ia])*(v/(float)cuts);
                    Color Tint(int u,int v) => inputColors.Length==input.Length
                        ? inputColors[ia]*(1-(u+v)/(float)cuts)+inputColors[ib]*(u/(float)cuts)+inputColors[ic]*(v/(float)cuts) : Color.white;
                    void Tri(int u0,int v0,int u1,int v1,int u2,int v2)
                    {
                        Vector3 a=Move(Point(u0,v0)),b=Move(Point(u1,v1)),c=Move(Point(u2,v2));
                        Vector3 n=Vector3.Cross(b-a,c-a).normalized;
                        int first=vertices.Count;
                        vertices.Add(a); vertices.Add(b); vertices.Add(c);
                        colors.Add(Tint(u0,v0)); colors.Add(Tint(u1,v1)); colors.Add(Tint(u2,v2));
                        for (int j=0;j<3;j++) { normals.Add(n); slots[slot].Add(first+j); }
                    }
                    for (int v=0;v<cuts;v++)
                    for (int u=0;u<cuts-v;u++)
                    {
                        Tri(u,v,u+1,v,u,v+1);
                        if (u+v<cuts-1) Tri(u+1,v,u+1,v+1,u,v+1);
                    }
                }
            }
            // 중복 정점을 위치로 용접해 내부 공유 간선을 제거한다.
            // 원본 옆면의 분할 수와 무관하게 윗면과 정확히 같은 경계선을 사용한다.
            var welded=new List<Vector3>();
            var lookup=new Dictionary<Vector3Int,int>();
            var edges=new Dictionary<(int,int),(int A,int B,int Count)>();
            int Weld(int index)
            {
                Vector3 p=vertices[index];
                var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
                if (!lookup.TryGetValue(key,out int id)) { id=welded.Count; welded.Add(p); lookup.Add(key,id); }
                vertices[index]=welded[id];
                return id;
            }
            void Edge(int a,int b)
            {
                var key=a<b ? (a,b) : (b,a);
                if (edges.TryGetValue(key,out var edge)) edges[key]=(edge.A,edge.B,edge.Count+1);
                else edges.Add(key,(a,b,1));
            }
            for (int i=0;i<slots[0].Count;i+=3)
            {
                int a=Weld(slots[0][i]),b=Weld(slots[0][i+1]),c=Weld(slots[0][i+2]);
                Edge(a,b); Edge(b,c); Edge(c,a);
            }
            void CloseTriangle(Vector3 a,Vector3 b,Vector3 c)
            {
                int first=vertices.Count;
                Vector3 n=Vector3.Cross(b-a,c-a).normalized;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                for (int j=0;j<3;j++) { normals.Add(n); colors.Add(Color.white); slots[1].Add(first+j); }
            }
            float bottom=source.bounds.min.y;
            Vector3 center=new Vector3(source.bounds.center.x,bottom,source.bounds.center.z);
            foreach (var edge in edges.Values)
            {
                if (edge.Count!=1) continue;
                Vector3 a=welded[edge.A],b=welded[edge.B];
                Vector3 da=new Vector3(a.x,bottom,a.z),db=new Vector3(b.x,bottom,b.z);
                CloseTriangle(b,a,da);
                CloseTriangle(b,da,db);
                CloseTriangle(center,db,da);
            }
            var mesh=new Mesh { name=source.name+"_"+mode, indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors);
            mesh.subMeshCount=slots.Length;
            for (int i=0;i<slots.Length;i++) mesh.SetTriangles(slots[i],i);
            mesh.RecalculateBounds(); return mesh;
        }
    }
}
