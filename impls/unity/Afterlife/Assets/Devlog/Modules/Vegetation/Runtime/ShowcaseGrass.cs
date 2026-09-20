using System;
using System.Collections.Generic;
using Afterlife.Devlog.Terrain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afterlife.Devlog.Day01
{
    // 군락은 고정 시드로 배치하고 각 quad의 바람/빌보드는 GPU에서 처리한다.
    public sealed class ShowcaseGrass : IDisposable
    {
        private readonly List<GameObject> _objects=new List<GameObject>();
        private readonly List<Mesh> _meshes=new List<Mesh>();
        private readonly Dictionary<Vector2Int,List<Vector3>> _afterGrassContacts=new Dictionary<Vector2Int,List<Vector3>>();
        private readonly List<ShowcaseVegetationDensity> _densities=new List<ShowcaseVegetationDensity>();
        public float FullDensityDistance { get; set; }=30;
        public float HalfDensityDistance { get; set; }=45;
        private sealed class Chunk
        {
            public readonly List<Vector3> Positions=new List<Vector3>(),Types=new List<Vector3>(),Normals=new List<Vector3>();
            public readonly List<Vector2> Uv=new List<Vector2>(),Sizes=new List<Vector2>();
            public readonly List<Color> Colors=new List<Color>();
            public readonly List<int> Indices=new List<int>();
        }
        private readonly List<GameObject> _suppressedDecorations=new List<GameObject>();
        private readonly Material _material;

        private static float Cluster(float x,float z) => Mathf.SmoothStep(0,1,
            Mathf.InverseLerp(.34f,.66f,Mathf.PerlinNoise(x*.62f+19.4f,z*.62f+31.8f)));
        private static float FlowerCluster(float x,float z) => Mathf.SmoothStep(0,1,
            Mathf.InverseLerp(.48f,.67f,Mathf.PerlinNoise(x*.91f+73.1f,z*.91f+8.6f)));

        public ShowcaseGrass(Transform stage) : this(new[] { stage }, true) { }
        public ShowcaseGrass(Transform[] stages) : this(stages, false) { }
        private ShowcaseGrass(Transform[] stages, bool singleStage)
        {
            var shader=Shader.Find("Afterlife/Devlog/ShowcaseGrass");
            if (shader==null) { Debug.LogError("Devlog grass shader is missing."); return; }
            _material=new Material(shader) { name="Devlog Perlin Grass" };
            for (int i=0;i<4;i++)
            {
                var texture=Resources.Load<Texture2D>("GrassSprites/Grass"+i);
                if (texture==null) { Debug.LogError("Devlog grass PNG missing: Grass"+i); return; }
                _material.SetTexture("_Grass"+i,texture);
            }
            if (singleStage) Build(stages[0],true);
            else for (int phase=1;phase<stages.Length;phase++) Build(stages[phase],phase==2);
        }

        // 게임 맵을 주입하는 경로는 Day01 계층 이름과 고정 지도에 의존하지 않는다.
        public sealed class Map
        {
            public int Radius;
            public int SamplesPerAxis=4;
            public Func<int,int,ShowcaseBiome> Biome;
            public Func<float,float,float> Height;
            public Func<float,float,Color> Color;
        }
        public ShowcaseGrass(Transform parent,Map map,Shader shader)
        {
            if(map==null || map.Biome==null || map.Height==null || map.Color==null)throw new ArgumentNullException(nameof(map));
            if(shader==null)throw new ArgumentNullException(nameof(shader));
            _material=new Material(shader){name="Injected biome grass"};
            for(int i=0;i<4;i++)_material.SetTexture("_Grass"+i,Resources.Load<Texture2D>("GrassSprites/Grass"+i));
            Build(parent,true,map);
        }
        public void SetExclusionMask(Texture texture,Vector4 bounds)
        { _material.SetTexture("_VegetationMask",texture);_material.SetVector("_VegetationBounds",bounds); }

        private void Build(Transform stage,bool forestEnabled,Map map=null)
        {
            var grassRandom=new System.Random(70421);
            var chunks=new Dictionary<Vector2Int,Chunk>();
            var obstacles=new List<Bounds>();
            var environment=map==null?stage.Find("Environment"):null;
            if (environment!=null)
                foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>())
                { var bounds=renderer.bounds; bounds.Expand(.28f); obstacles.Add(bounds); }
            var resources=map==null?stage.Find("ClickableResources"):null;
            var flatCells=new HashSet<Vector2Int>();
            var tileColors=new Dictionary<Vector2Int,Color>();
            Texture2D blendMap=null;
            Vector4 blendBounds=Vector4.zero;
            var grid=map==null?stage.Find("Terrain_Flat/BiomeGrid_12x10"):null;
            if (grid!=null)
                foreach (Transform tile in grid)
                {
                    var tileRenderer=tile.GetComponent<MeshRenderer>();
                    var tileMaterial=tileRenderer!=null ? tileRenderer.sharedMaterial : null;
                    if (tileMaterial!=null && tileMaterial.HasProperty("_TerrainBlend") && tileMaterial.GetFloat("_TerrainBlend")>.5f)
                    {
                        blendMap=tileMaterial.GetTexture("_TerrainColorMap") as Texture2D;
                        blendBounds=tileMaterial.GetVector("_TerrainMapBounds");
                    }
                    if (tileRenderer!=null && tileRenderer.sharedMaterial!=null && tileRenderer.sharedMaterial.HasProperty("_BaseColor"))
                        tileColors[new Vector2Int(Mathf.FloorToInt(tile.localPosition.x),Mathf.FloorToInt(tile.localPosition.z))]=tileRenderer.sharedMaterial.GetColor("_BaseColor");
                    var filter=tile.GetComponent<MeshFilter>();
                    if (filter!=null && filter.sharedMesh!=null && filter.sharedMesh.name.EndsWith("_Flat"))
                        flatCells.Add(new Vector2Int(Mathf.FloorToInt(tile.localPosition.x),Mathf.FloorToInt(tile.localPosition.z)));
                }
            for (int z=map==null?-5:-map.Radius;z<(map==null?5:map.Radius+1);z++)
            for (int x=map==null?-6:-map.Radius;x<(map==null?6:map.Radius+1);x++)
            {
                var biome=map==null?ShowcaseBiomeMap.At(x,z):map.Biome(x,z);
                bool forest=forestEnabled && biome==ShowcaseBiome.Forest;
                if (biome!=ShowcaseBiome.Grass && !forest) continue;
                var random=forest ? new System.Random(ShowcaseTileVariants.Seed(x,z)) : grassRandom;
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
                    float terrainHeight=map!=null?map.Height(px,pz):flatCells.Contains(new Vector2Int(x,z)) ? 0 : (float)ShowcaseTileVariants.Height(x,z,px-x,pz-z);
                    Vector3 local=new Vector3(px,terrainHeight+.007f,pz), world=stage.TransformPoint(local);
                    bool blocked=false;
                    foreach (var bounds in obstacles)
                        if (world.x>=bounds.min.x && world.x<=bounds.max.x && world.z>=bounds.min.z && world.z<=bounds.max.z)
                        { blocked=true; break; }
                    if (!blocked && resources!=null)
                        foreach (Transform resource in resources)
                        {
                            Vector3 delta=resource.position-world; delta.y=0;
                            if (delta.sqrMagnitude<.32f*.32f) { blocked=true; break; }
                        }
                    if (blocked) continue;
                    int sprite=random.Next(4);
                    float pixelSize=.020f+(float)random.NextDouble()*.009f;
                    float width=(sprite==0 ? 9 : sprite==1 ? 5 : sprite==2 ? 14 : 8)*pixelSize;
                    float height=(sprite==0 ? 9 : sprite==1 ? 4 : sprite==2 ? 9 : 14)*pixelSize;
                    // 기본 풀 94%, 키 큰 풀 2%, 검은 풀 1%, 흰 풀 1%, 꽃 2%.
                    float rare=(float)random.NextDouble();
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
                    // 실제 생성된 After 풀의 위치/폭만 접촉 대상으로 보관한다. 빈 군락과 장애물은 제외된다.
                    if (forestEnabled && map==null)
                    {
                        var contactCell=new Vector2Int(Mathf.FloorToInt(world.x),Mathf.FloorToInt(world.z));
                        if (!_afterGrassContacts.TryGetValue(contactCell,out var contacts))
                        { contacts=new List<Vector3>(); _afterGrassContacts.Add(contactCell,contacts); }
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
                        float dx=(float)(ShowcaseTileVariants.Height(x,z,px-x+.005,pz-z)-ShowcaseTileVariants.Height(x,z,px-x-.005,pz-z))/.01f;
                        float dz=(float)(ShowcaseTileVariants.Height(x,z,px-x,pz-z+.005)-ShowcaseTileVariants.Height(x,z,px-x,pz-z-.005))/.01f;
                        normal=new Vector3(-dx,1,-dz).normalized;
                    }
                    var chunkCell=new Vector2Int(Mathf.FloorToInt(px/4),Mathf.FloorToInt(pz/4));
                    if (!chunks.TryGetValue(chunkCell,out var chunk)) { chunk=new Chunk(); chunks.Add(chunkCell,chunk); }
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
            foreach (var entry in chunks)
            {
                var chunk=entry.Value;
                ShowcaseVegetationDensity.ShuffleCards(chunk.Indices,70421);
                var mesh=new Mesh { name="Batched Grass Chunk", indexFormat=IndexFormat.UInt32 };
                mesh.SetVertices(chunk.Positions); mesh.SetUVs(0,chunk.Uv); mesh.SetUVs(1,chunk.Sizes); mesh.SetUVs(2,chunk.Types);
                mesh.SetColors(chunk.Colors); mesh.SetNormals(chunk.Normals); mesh.SetTriangles(chunk.Indices,0);
                var bounds=mesh.bounds; bounds.Expand(2f); mesh.bounds=bounds;
                var go=new GameObject("BillboardGrass_"+entry.Key.x+"_"+entry.Key.y); go.transform.SetParent(stage,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var rendererOut=go.AddComponent<MeshRenderer>(); rendererOut.sharedMaterial=_material;
                rendererOut.shadowCastingMode=ShadowCastingMode.Off;
                _objects.Add(go); _meshes.Add(mesh);
                _densities.Add(new ShowcaseVegetationDensity(mesh,rendererOut,chunk.Indices.Count));
            }
            if (forestEnabled && map==null)
                foreach (var decoration in stage.GetComponentsInChildren<Transform>(true))
                {
                    bool flowers=decoration.name=="Flowers_Pivot" || decoration.name.StartsWith("TileDecoration_Flowers_");
                    bool ferns=decoration.name=="Fern_Pivot" || decoration.name.StartsWith("TileDecoration_Ferns_");
                    if ((!flowers && !ferns) || !decoration.gameObject.activeSelf) continue;
                    var point=stage.InverseTransformPoint(decoration.position);
                    if ((flowers ? FlowerCluster(point.x,point.z) : Cluster(point.x,point.z))>=.3f) continue;
                    _suppressedDecorations.Add(decoration.gameObject);
                    decoration.gameObject.SetActive(false);
                }
        }

        public bool HasGrassAt(Vector3 worldPoint)
        {
            int x=Mathf.FloorToInt(worldPoint.x),z=Mathf.FloorToInt(worldPoint.z);
            for (int dzCell=-1;dzCell<=1;dzCell++)
            for (int dxCell=-1;dxCell<=1;dxCell++)
            {
                if (!_afterGrassContacts.TryGetValue(new Vector2Int(x+dxCell,z+dzCell),out var contacts)) continue;
                foreach (var contact in contacts)
                {
                    float dx=worldPoint.x-contact.x, dz=worldPoint.z-contact.y;
                    if (dx*dx+dz*dz<=contact.z*contact.z) return true;
                }
            }
            return false;
        }

        public void Tick(Camera camera)
        {
            if (camera!=null) foreach (var density in _densities) density.Tick(camera,FullDensityDistance,HalfDensityDistance);
        }

        public void Dispose()
        {
            foreach (var decoration in _suppressedDecorations) if (decoration!=null) decoration.SetActive(true);
            _suppressedDecorations.Clear();
            foreach (var go in _objects) UnityEngine.Object.Destroy(go);
            foreach (var mesh in _meshes) UnityEngine.Object.Destroy(mesh);
            if (_material!=null) UnityEngine.Object.Destroy(_material);
            _objects.Clear(); _meshes.Clear();
            _densities.Clear();
            _afterGrassContacts.Clear();
        }
    }
}
