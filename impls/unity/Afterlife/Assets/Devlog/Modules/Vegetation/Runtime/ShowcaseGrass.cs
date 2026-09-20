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
        private readonly List<Vector3> _afterGrassContacts=new List<Vector3>();
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

        private void Build(Transform stage,bool forestEnabled)
        {
            var grassRandom=new System.Random(70421);
            var positions=new List<Vector3>(); var uv=new List<Vector2>();
            var sizes=new List<Vector2>(); var indices=new List<int>();
            var colors=new List<Color>(); var types=new List<Vector3>(); var normals=new List<Vector3>();
            var obstacles=new List<Bounds>();
            var environment=stage.Find("Environment");
            if (environment!=null)
                foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>())
                { var bounds=renderer.bounds; bounds.Expand(.28f); obstacles.Add(bounds); }
            var resources=stage.Find("ClickableResources");
            var flatCells=new HashSet<Vector2Int>();
            var tileColors=new Dictionary<Vector2Int,Color>();
            Texture2D blendMap=null;
            Vector4 blendBounds=Vector4.zero;
            var grid=stage.Find("Terrain_Flat/BiomeGrid_12x10");
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
            for (int z=-5;z<5;z++)
            for (int x=-6;x<6;x++)
            {
                var biome=ShowcaseBiomeMap.At(x,z);
                bool forest=forestEnabled && biome==ShowcaseBiome.Forest;
                if (biome!=ShowcaseBiome.Grass && !forest) continue;
                var random=forest ? new System.Random(ShowcaseTileVariants.Seed(x,z)) : grassRandom;
                for (int j=0;j<7;j++)
                for (int i=0;i<7;i++)
                {
                    float px=x+.055f+(i+(float)random.NextDouble()*.8f)*.128f;
                    float pz=z+.055f+(j+(float)random.NextDouble()*.8f)*.128f;
                    float density=Mathf.PerlinNoise(px*.65f+19.4f,pz*.65f+31.8f);
                    float probability=forestEnabled ? Mathf.Lerp(.035f,forest ? .94f : .88f,Cluster(px,pz))
                        : Mathf.Lerp(.35f,.95f,density);
                    if (random.NextDouble()>probability) continue;
                    float terrainHeight=flatCells.Contains(new Vector2Int(x,z)) ? 0 : (float)ShowcaseTileVariants.Height(x,z,px-x,pz-z);
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
                    if (forestEnabled) _afterGrassContacts.Add(new Vector3(world.x,world.z,width*.5f+.04f));
                    float toneNoise=Mathf.PerlinNoise(px*.8f+47.2f,pz*.8f+13.7f);
                    float tone=toneNoise<.42f ? .88f : toneNoise<.58f ? 1 : 1.12f;
                    Color ground=tileColors.TryGetValue(new Vector2Int(x,z),out var tint) ? tint : new Color(.30f,.40f,.23f);
                    if (blendMap!=null && blendMap.isReadable)
                        ground=blendMap.GetPixel(Mathf.Clamp(Mathf.FloorToInt((world.x-blendBounds.x)/blendBounds.z*blendMap.width),0,blendMap.width-1),
                            Mathf.Clamp(Mathf.FloorToInt((world.z-blendBounds.y)/blendBounds.w*blendMap.height),0,blendMap.height-1));
                    Vector3 normal=Vector3.up;
                    if (!flatCells.Contains(new Vector2Int(x,z)))
                    {
                        float dx=(float)(ShowcaseTileVariants.Height(x,z,px-x+.005,pz-z)-ShowcaseTileVariants.Height(x,z,px-x-.005,pz-z))/.01f;
                        float dz=(float)(ShowcaseTileVariants.Height(x,z,px-x,pz-z+.005)-ShowcaseTileVariants.Height(x,z,px-x,pz-z-.005))/.01f;
                        normal=new Vector3(-dx,1,-dz).normalized;
                    }
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
            var mesh=new Mesh { name="Batched Grass Quads", indexFormat=IndexFormat.UInt32 };
            mesh.SetVertices(positions); mesh.SetUVs(0,uv); mesh.SetUVs(1,sizes); mesh.SetUVs(2,types);
            mesh.SetColors(colors); mesh.SetNormals(normals); mesh.SetTriangles(indices,0);
            mesh.bounds=new Bounds(new Vector3(0,.4f,0),new Vector3(13,2,11));
            var go=new GameObject("BillboardGrass"); go.transform.SetParent(stage,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var rendererOut=go.AddComponent<MeshRenderer>(); rendererOut.sharedMaterial=_material;
            rendererOut.shadowCastingMode=ShadowCastingMode.Off;
            _objects.Add(go); _meshes.Add(mesh);
            if (forestEnabled)
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
            foreach (var contact in _afterGrassContacts)
            {
                float dx=worldPoint.x-contact.x, dz=worldPoint.z-contact.y;
                if (dx*dx+dz*dz<=contact.z*contact.z) return true;
            }
            return false;
        }

        public void Dispose()
        {
            foreach (var decoration in _suppressedDecorations) if (decoration!=null) decoration.SetActive(true);
            _suppressedDecorations.Clear();
            foreach (var go in _objects) UnityEngine.Object.Destroy(go);
            foreach (var mesh in _meshes) UnityEngine.Object.Destroy(mesh);
            if (_material!=null) UnityEngine.Object.Destroy(_material);
            _objects.Clear(); _meshes.Clear();
            _afterGrassContacts.Clear();
        }
    }
}
