using System;
using System.Collections.Generic;
using UnityEngine;
using Afterlife.Modules.Terrain;
namespace Afterlife.Modules.Vegetation
{
        public sealed class GrassChunkData
        {
            public readonly List<Vector3> Positions=new List<Vector3>(),Types=new List<Vector3>(),Normals=new List<Vector3>();
            public readonly List<Vector2> Uv=new List<Vector2>(),Sizes=new List<Vector2>();
            public readonly List<Color> Colors=new List<Color>();
            public readonly List<int> Indices=new List<int>();
        }
        public class GrassMap
        {
            public int Radius;
            public int SamplesPerAxis=4;
            public float AccentFraction=1;
            public Func<int,int,TerrainBiome> Biome;
            public Func<float,float,float> Height;
            public Func<float,float,Color> Color;
        }
        public class GrassStageLayout
        {
            public int MinX,MinZ,Width,Depth;
            public string EnvironmentPath,ResourcesPath,GridPath;
            public bool EnableDecorations=true;
            public Func<int,int,TerrainBiome> Biome;
            // null이면 장식이 아니며, true는 꽃, false는 고사리다.
            public Func<Transform,bool?> DecorationFlower;
        }
    public sealed class GrassGeometryData
    {
        public readonly Dictionary<Vector2Int,GrassChunkData> Chunks=new Dictionary<Vector2Int,GrassChunkData>();
        public readonly Dictionary<Vector2Int,List<Vector3>> Contacts=new Dictionary<Vector2Int,List<Vector3>>();
    }
    // 계산 결과만 반환한다. Mesh/GameObject 생성과 Inspector는 호출자가 담당한다.
    public static class GrassGeometry
    {
        public static float Cluster(float x,float z) => Mathf.SmoothStep(0,1,
            Mathf.InverseLerp(.34f,.66f,Mathf.PerlinNoise(x*.62f+19.4f,z*.62f+31.8f)));
        public static float FlowerCluster(float x,float z) => Mathf.SmoothStep(0,1,
            Mathf.InverseLerp(.48f,.67f,Mathf.PerlinNoise(x*.91f+73.1f,z*.91f+8.6f)));

        public static GrassGeometryData Calculate(GrassMap map,GrassStageLayout layout,Matrix4x4 localToWorld,bool forestEnabled,
            IReadOnlyList<Bounds> obstacles=null,IReadOnlyList<Vector3> resourcePositions=null,HashSet<Vector2Int> flatCells=null,Dictionary<Vector2Int,Color> tileColors=null,Texture2D blendMap=null,Vector4 blendBounds=default)
        {
            if(map==null && layout==null)throw new ArgumentException("Map or authored layout required.");
            obstacles=obstacles??Array.Empty<Bounds>();flatCells=flatCells??new HashSet<Vector2Int>();tileColors=tileColors??new Dictionary<Vector2Int,Color>();
            var result=new GrassGeometryData();var chunks=result.Chunks;var grassRandom=new System.Random(70421);
            for (int z=map==null?layout.MinZ:-map.Radius;z<(map==null?layout.MinZ+layout.Depth:map.Radius+1);z++)
            for (int x=map==null?layout.MinX:-map.Radius;x<(map==null?layout.MinX+layout.Width:map.Radius+1);x++)
            {
                var biome=map==null?layout.Biome(x,z):map.Biome(x,z);
                bool forest=forestEnabled && biome==TerrainBiome.Forest;
                if (biome!=TerrainBiome.Grass && !forest) continue;
                var random=forest ? new System.Random(TerrainTileVariants.Seed(x,z)) : grassRandom;
                int samples=map==null?7:Mathf.Clamp(map.SamplesPerAxis,1,7);
                float spacing=map==null?.128f:.896f/samples;
                for (int j=0;j<samples;j++)
                for (int i=0;i<samples;i++)
                {
                    float px=x+.055f+(i+(float)random.NextDouble()*.8f)*spacing;
                    float pz=z+.055f+(j+(float)random.NextDouble()*.8f)*spacing;
                    if(map!=null){px-=.5f;pz-=.5f;}
                    float density=Mathf.PerlinNoise(px*.65f+19.4f,pz*.65f+31.8f);
                    float probability=forestEnabled ? Mathf.Lerp(.035f,forest ? .94f : .88f,Cluster(px,pz))
                        : Mathf.Lerp(.35f,.95f,density);
                    if (random.NextDouble()>probability) continue;
                    float terrainHeight=map!=null?map.Height(px,pz):flatCells.Contains(new Vector2Int(x,z)) ? 0 : (float)TerrainTileVariants.Height(x,z,px-x,pz-z);
                    Vector3 local=new Vector3(px,terrainHeight+.007f,pz), world=localToWorld.MultiplyPoint3x4(local);
                    bool blocked=false;
                    foreach (var bounds in obstacles)
                        if (world.x>=bounds.min.x && world.x<=bounds.max.x && world.z>=bounds.min.z && world.z<=bounds.max.z)
                        { blocked=true; break; }
                    if (!blocked && resourcePositions!=null)
                        foreach (Vector3 resource in resourcePositions)
                        {
                            Vector3 delta=resource-world; delta.y=0;
                            if (delta.sqrMagnitude<.32f*.32f) { blocked=true; break; }
                        }
                    if (blocked) continue;
                    int sprite=random.Next(4);
                    float pixelSize=.020f+(float)random.NextDouble()*.009f;
                    float width=(sprite==0 ? 9 : sprite==1 ? 5 : sprite==2 ? 14 : 8)*pixelSize;
                    float height=(sprite==0 ? 9 : sprite==1 ? 4 : sprite==2 ? 9 : 14)*pixelSize;
                    // 기본 풀 94%, 키 큰 풀 2%, 검은 풀 1%, 흰 풀 1%, 꽃 2%.
                    float rare=(float)random.NextDouble();
                    // 배치/스프라이트 난수는 유지하고 변종만 기본 풀로 전환한다.
                    if(map!=null)rare=map.AccentFraction<=0?2:rare/Mathf.Clamp01(map.AccentFraction);
                    else if(!layout.EnableDecorations)rare=2;
                    int kind=rare<.02f ? 1 : rare<.03f ? 2 : rare<.04f ? 3 : rare<.06f ? 4 : 0;
                    if (forestEnabled && !forest)
                        kind=rare<.02f ? 1 : rare<.03f ? 2 : rare<.04f ? 3
                            : rare<.04f+.18f*FlowerCluster(px,pz) ? 4 : 0;
                    // 숲 전용: 키 큰 청록풀 8%, 낮은 이끼빛 풀 6%, 자줏빛 끝 풀 3%.
                    if (forest) kind=rare<.08f ? 5 : rare<.14f ? 6 : rare<.17f ? 7 : 0;
                    if (kind==5) { height*=1.6f; width*=.85f; }
                    if (kind==6) { height*=.6f; width*=1.3f; }
                    if (kind==7) height*=1.15f;
                    if (kind==4) width=.20f;
                    if (kind==1) height*=1.55f;
                    if (kind==2 || kind==3) { height*=.55f; width*=.85f; }
                    // Day01 After와 주입된 게임 맵 모두 생성된 풀의 위치/폭을 접촉 대상으로 보관한다.
                    if (forestEnabled)
                    {
                        var contactCell=new Vector2Int(Mathf.FloorToInt(world.x),Mathf.FloorToInt(world.z));
                        if (!result.Contacts.TryGetValue(contactCell,out var contacts))
                        { contacts=new List<Vector3>(); result.Contacts.Add(contactCell,contacts); }
                        contacts.Add(new Vector3(world.x,world.z,width*.5f+.04f));
                    }
                    float toneNoise=Mathf.PerlinNoise(px*.8f+47.2f,pz*.8f+13.7f);
                    float tone=toneNoise<.42f ? .88f : toneNoise<.58f ? 1 : 1.12f;
                    Color ground=tileColors.TryGetValue(new Vector2Int(x,z),out var tint) ? tint : new Color(.30f,.40f,.23f);
                    if (blendMap!=null && blendMap.isReadable)
                        ground=blendMap.GetPixel(Mathf.Clamp(Mathf.FloorToInt((world.x-blendBounds.x)/blendBounds.z*blendMap.width),0,blendMap.width-1),
                            Mathf.Clamp(Mathf.FloorToInt((world.z-blendBounds.y)/blendBounds.w*blendMap.height),0,blendMap.height-1));
                    if(map!=null)ground=map.Color(px,pz);
                    Vector3 normal=Vector3.up;
                    if (map!=null)
                    {
                        normal=new Vector3(map.Height(px-.005f,pz)-map.Height(px+.005f,pz),.01f,map.Height(px,pz-.005f)-map.Height(px,pz+.005f)).normalized;
                    }
                    else if (!flatCells.Contains(new Vector2Int(x,z)))
                    {
                        float dx=(float)(TerrainTileVariants.Height(x,z,px-x+.005,pz-z)-TerrainTileVariants.Height(x,z,px-x-.005,pz-z))/.01f;
                        float dz=(float)(TerrainTileVariants.Height(x,z,px-x,pz-z+.005)-TerrainTileVariants.Height(x,z,px-x,pz-z-.005))/.01f;
                        normal=new Vector3(-dx,1,-dz).normalized;
                    }
                    var chunkCell=new Vector2Int(Mathf.FloorToInt(px/4),Mathf.FloorToInt(pz/4));
                    if (!chunks.TryGetValue(chunkCell,out var chunk)) { chunk=new GrassChunkData(); chunks.Add(chunkCell,chunk); }
                    var positions=chunk.Positions; var uv=chunk.Uv; var sizes=chunk.Sizes;
                    var colors=chunk.Colors; var types=chunk.Types; var normals=chunk.Normals; var indices=chunk.Indices;
                    int first=positions.Count;
                    for (int corner=0;corner<4;corner++)
                    {
                        positions.Add(local);
                        uv.Add(new Vector2(corner==1 || corner==2 ? 1 : 0,corner>=2 ? 1 : 0));
                        sizes.Add(new Vector2(width,height));
                        colors.Add(ground); types.Add(new Vector3(tone,kind,sprite)); normals.Add(normal);
                    }
                    indices.Add(first); indices.Add(first+2); indices.Add(first+1);
                    indices.Add(first); indices.Add(first+3); indices.Add(first+2);
                }
            }
            return result;
        }
    }
}
