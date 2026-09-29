using System;
using System.Collections.Generic;
using Afterlife.Modules.Terrain;
using UnityEngine;

namespace Afterlife.Modules.TerrainView
{
    // 맵과 재질은 주입받는다. 게임 상태/입력/시야를 소유하지 않는 지형 표현 모듈.
    public sealed class TerrainGeometry
    {
        public const float BaseHeight=-.06f;
        private readonly Func<int,int,TerrainBiome> _biome;
        private readonly TerrainHeightGrid _field;
        private readonly CoastalTerrain _coast;
        private readonly Func<int,int,bool> _water;
        private readonly HashSet<Vector2Int> _coastalTiles=new HashSet<Vector2Int>();
        
        
        public readonly List<Chunk> Chunks=new List<Chunk>();
        private readonly Color[] _palette;
        private readonly double[] _weights=new double[7];
        private readonly Func<int,int,TerrainBiome> _clampedBiome;
        // Height는 항상 1/4 또는 1/16 격자의 꼭짓점을 조회한다. 맵 수명 동안 정확한 값을 재사용한다.
        private readonly Dictionary<Vector2Int,float> _nodeHeights=new Dictionary<Vector2Int,float>();
        public sealed class Chunk
        {
            public Bounds Bounds;
            public Vector3[] Normals;
            public string Name;
            public Vector3[] Vertices;
            public int[] Indices;
            public Vector3[] Deformed;
            public Color[] Colors;
        }
        private readonly Func<float,float,float> _height;
        public TerrainGeometry(int radius,Func<int,int,TerrainBiome> biome,Color[] palette,CoastalTerrain coast=null,Func<float,float,float> height=null)
        {
            _height=height;
            _coast=coast;
            _water=(x,z)=>Math.Abs(x)<=radius && Math.Abs(z)<=radius && biome(x,z)==TerrainBiome.Water;
            if(coast!=null && coast.Shape!=CoastShape.Square)
                for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)
                    if(!_water(x,z))
                        for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                            if(_water(x+dx,z+dz))_coastalTiles.Add(new Vector2Int(x,z));
            _biome=biome; _field=new TerrainHeightGrid(radius); _palette=palette;
            _clampedBiome=(x,z)=>biome(Mathf.Clamp(x,-radius,radius),Mathf.Clamp(z,-radius,radius));
            for(int z=-radius;z<=radius;z+=8) for(int x=-radius;x<=radius;x+=8)
                Build(x,z,Math.Min(x+7,radius),Math.Min(z+7,radius),radius);
        }
        private int Segments(int x,int z)=>_coastalTiles.Contains(new Vector2Int(x,z))?16:4;
        private float Node(float x,float z)=>BaseHeight+(_height!=null?_height(x,z):(float)_field.Height(x,z))
            +(_coast==null?0:(float)CoastField.Offset(x+.5,z+.5,_water,_coast.Shape));
        private float GridNode(int x,int z,int segments)
        {
            var key=new Vector2Int(x*(16/segments),z*(16/segments));
            if(!_nodeHeights.TryGetValue(key,out float height))
            {
                height=Node(x/(float)segments,z/(float)segments);
                _nodeHeights.Add(key,height);
            }
            return height;
        }
        public float Height(float x,float z)
        {
            int tx=Mathf.FloorToInt(x+.5f),tz=Mathf.FloorToInt(z+.5f);
            if(_biome(tx,tz)==TerrainBiome.Water)return _coast?.WaterHeight ?? BaseHeight;
            int n=Segments(tx,tz),ix=Mathf.FloorToInt(x*n),iz=Mathf.FloorToInt(z*n);
            float u=x*n-ix,v=z*n-iz;
            float a=GridNode(ix,iz,n),b=GridNode(ix,iz+1,n);
            float c=GridNode(ix+1,iz,n),d=GridNode(ix+1,iz+1,n);
            if(TerrainHeightField.Alternate(ix,iz))return u+v<=1?a+(c-a)*u+(b-a)*v:d+(b-d)*(1-u)+(c-d)*(1-v);
            return v>=u?a+(d-b)*u+(b-a)*v:a+(c-a)*u+(d-c)*v;
        }
        public Color SurfaceColor(float x,float z)=>Tint(x,z,false);
        private Color Tint(float x,float z,bool water)
        {
            if(water) { var waterColor=_palette[(int)(_coast==null?TerrainBiome.Water:TerrainBiome.Sand)]*(_coast==null?1:.65f); waterColor.a=_coast==null?1:-1; return waterColor; }
            BiomeColorBlend.Weights(x+.5,z+.5,_weights,_clampedBiome);
            var color=Color.clear;
            for(int i=0;i<_weights.Length;i++) color+=_palette[i]*(float)_weights[i];
            color.a=0; // 육지 0, 기존 수면 1, 분리된 해저 -1. 불투명도는 재질에서 지정한다.
            return color;
        }
        private void Build(int minX,int minZ,int maxX,int maxZ,int radius)
        {
            var vertices=new List<Vector3>(); var normals=new List<Vector3>();
            var colors=new List<Color>(); var indices=new List<int>();
            for(int z=minZ;z<=maxZ;z++) for(int x=minX;x<=maxX;x++)
            {
                bool water=_biome(x,z)==TerrainBiome.Water;
                int start=vertices.Count,n=Segments(x,z),stride=n+1;
                for(int iz=0;iz<=n;iz++) for(int ix=0;ix<=n;ix++)
                {
                    float px=x-.5f+ix/(float)n,pz=z-.5f+iz/(float)n;
                    float h=water ? (_coast?.FloorHeight ?? BaseHeight) : GridNode(x*n-n/2+ix,z*n-n/2+iz,n);
                    vertices.Add(new Vector3(px,h,pz)); colors.Add(Tint(px,pz,water));
                    normals.Add(water ? Vector3.up : new Vector3(Node(px-.01f,pz)-Node(px+.01f,pz),.02f,
                        Node(px,pz-.01f)-Node(px,pz+.01f)).normalized);
                }
                for(int iz=0;iz<n;iz++) for(int ix=0;ix<n;ix++)
                {
                    int a=start+iz*stride+ix,b=a+stride,c=a+1,d=b+1;
                    if(TerrainHeightField.Alternate(x*n-n/2+ix,z*n-n/2+iz))
                    { indices.Add(a);indices.Add(b);indices.Add(c);indices.Add(c);indices.Add(b);indices.Add(d); }
                    else { indices.Add(a);indices.Add(b);indices.Add(d);indices.Add(a);indices.Add(d);indices.Add(c); }
                }
                // 물가 높이차와 맵 외곽 단면만 닫는다. 육지 내부 공유 경계에는 벽을 만들지 않는다.
                for(int side=0;side<4;side++)
                {
                    int dx=side==0?-1:side==1?1:0,dz=side==2?-1:side==3?1:0;
                    bool outside=Math.Abs(x+dx)>radius || Math.Abs(z+dz)>radius;
                    if(!outside && (water || _biome(x+dx,z+dz)!=TerrainBiome.Water)) continue;
                    for(int step=0;step<n;step++)
                    {
                        int p=side==0?start+step*stride:side==1?start+n+step*stride:side==2?start+step:start+n*stride+step;
                        int q=p+(side<2?stride:1),first=vertices.Count;
                        var a=vertices[p];var b=vertices[q];float floor=outside?BaseHeight-.8f:(_coast?.FloorHeight ?? BaseHeight);
                        vertices.Add(a);vertices.Add(b);vertices.Add(new Vector3(a.x,floor,a.z));vertices.Add(new Vector3(b.x,floor,b.z));
                        for(int k=0;k<4;k++){normals.Add(new Vector3(dx,0,dz));colors.Add(colors[k%2==0?p:q]);}
                        indices.Add(first);indices.Add(first+1);indices.Add(first+2);indices.Add(first+2);indices.Add(first+1);indices.Add(first+3);
                    }
                }
            }
            var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var vertex in vertices)bounds.Encapsulate(vertex);
            Chunks.Add(new Chunk { Name="TerrainChunk_"+minX+"_"+minZ,Bounds=bounds,Vertices=vertices.ToArray(),Indices=indices.ToArray(),Colors=colors.ToArray(),Normals=normals.ToArray() });
        }
        // 물리 월드에 지형 Collider를 추가하지 않고 렌더링과 같은 삼각형을 선택한다.
        public bool Raycast(Ray ray,out Vector3 point,Func<Vector3,float> heightOffset=null)
        {
            float nearest=float.PositiveInfinity;
            if(_coast!=null && new Plane(Vector3.up,new Vector3(0,_coast.WaterHeight,0)).Raycast(ray,out float waterDistance))
            {
                var hit=ray.GetPoint(waterDistance);
                if(_water(Mathf.FloorToInt(hit.x+.5f),Mathf.FloorToInt(hit.z+.5f)))nearest=waterDistance;
            }
            foreach(var chunk in Chunks)
            {
                var bounds=chunk.Bounds;
                if(heightOffset!=null)bounds.Expand(.22f);
                if(!bounds.IntersectRay(ray,out float entry) || entry>nearest) continue;
                var vertices=chunk.Vertices;
                if(heightOffset!=null)
                {
                    if(chunk.Deformed==null)chunk.Deformed=new Vector3[vertices.Length];
                    for(int j=0;j<vertices.Length;j++)
                    {
                        var vertex=vertices[j];
                        if(chunk.Colors[j].a>=0 && chunk.Colors[j].a<.5f)
                            vertex.y+=heightOffset(vertex);
                        chunk.Deformed[j]=vertex;
                    }
                    vertices=chunk.Deformed;
                }
                for(int i=0;i<chunk.Indices.Length;i+=3)
                {
                    var a=vertices[chunk.Indices[i]];
                    var ab=vertices[chunk.Indices[i+1]]-a;var ac=vertices[chunk.Indices[i+2]]-a;
                    var p=Vector3.Cross(ray.direction,ac);float det=Vector3.Dot(ab,p);
                    if(Mathf.Abs(det)<1e-8f) continue;
                    var offset=ray.origin-a;float u=Vector3.Dot(offset,p)/det;
                    if(u<0 || u>1) continue;
                    var q=Vector3.Cross(offset,ab);float v=Vector3.Dot(ray.direction,q)/det;
                    if(v<0 || u+v>1) continue;
                    float t=Vector3.Dot(ac,q)/det;
                    if(t>=0 && t<nearest) nearest=t;
                }
            }
            point=float.IsPositiveInfinity(nearest)?default:ray.GetPoint(nearest);
            return !float.IsPositiveInfinity(nearest);
        }
    }
}
