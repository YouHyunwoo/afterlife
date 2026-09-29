using System;
using System.Collections.Generic;
using Afterlife.Modules.Terrain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afterlife.Modules.Vegetation
{
    // 군락은 고정 시드로 배치하고 각 quad의 바람/빌보드는 GPU에서 처리한다.
    public sealed class GrassRenderer : IDisposable
    {
        private readonly List<GameObject> _objects=new List<GameObject>();
        private readonly List<Mesh> _meshes=new List<Mesh>();
        private readonly Dictionary<Vector2Int,List<Vector3>> _afterGrassContacts=new Dictionary<Vector2Int,List<Vector3>>();
        private readonly List<VegetationDensityController> _densities=new List<VegetationDensityController>();
        public float FullDensityDistance { get; set; }=30;
        public float HalfDensityDistance { get; set; }=45;
        private readonly List<GameObject> _suppressedDecorations=new List<GameObject>();
        private readonly Material _material;

        public static bool DecorationVisible(bool flowers,float x,float z)
            => (flowers?GrassGeometry.FlowerCluster(x,z):GrassGeometry.Cluster(x,z))>=.3f;

        public sealed class StageLayout : GrassStageLayout { }
        private readonly StageLayout _stageLayout;
        public GrassRenderer(Transform stage,StageLayout layout) : this(new[] { stage }, true, layout) { }
        public GrassRenderer(Transform[] stages,StageLayout layout) : this(stages, false, layout) { }
        private GrassRenderer(Transform[] stages, bool singleStage,StageLayout layout)
        {
            if (layout==null || layout.Biome==null || layout.Width<=0 || layout.Depth<=0
                || string.IsNullOrEmpty(layout.EnvironmentPath) || string.IsNullOrEmpty(layout.ResourcesPath)
                || string.IsNullOrEmpty(layout.GridPath))
                throw new ArgumentException("A biome, positive dimensions, and stage paths are required.",nameof(layout));
            _stageLayout=layout;
            var shader=Shader.Find("Afterlife/Vegetation/Grass");
            if (shader==null) { Debug.LogError("Afterlife grass shader is missing."); return; }
            _material=new Material(shader) { name="Afterlife Perlin Grass" };
            for (int i=0;i<4;i++)
            {
                var texture=Resources.Load<Texture2D>("GrassSprites/Grass"+i);
                if (texture==null) { Debug.LogError("Afterlife grass PNG missing: Grass"+i); return; }
                _material.SetTexture("_Grass"+i,texture);
            }
            if (singleStage) Build(stages[0],true);
            else for (int phase=1;phase<stages.Length;phase++) Build(stages[phase],phase==2);
        }

        // 게임 맵은 별도 표현 설정으로 주입한다.
        public sealed class Map : GrassMap { }
        public GrassRenderer(Transform parent,Map map,Shader shader)
        {
            if(map==null || map.Biome==null || map.Height==null || map.Color==null)throw new ArgumentNullException(nameof(map));
            if(shader==null)throw new ArgumentNullException(nameof(shader));
            _material=new Material(shader){name="Injected biome grass"};
            for(int i=0;i<4;i++)_material.SetTexture("_Grass"+i,Resources.Load<Texture2D>("GrassSprites/Grass"+i));
            Build(parent,true,map);
        }
        private float _sizeScale=1;
        public void SetSizeScale(float value)
        {
            _sizeScale=Mathf.Clamp(value,.1f,1.5f);
            SetFloat("_GrassSizeScale",_sizeScale);
        }
        public void SetColor(string property,Color value)
        { if(_material!=null && _material.GetColor(property)!=value)_material.SetColor(property,value); }
        public void SetFloat(string property,float value)
        { if(_material!=null && _material.GetFloat(property)!=value)_material.SetFloat(property,value); }
        public void SetExclusionMask(Texture texture,Vector4 bounds)
        { _material.SetTexture("_VegetationMask",texture);_material.SetVector("_VegetationBounds",bounds); }

        private void Build(Transform stage,bool forestEnabled,Map map=null)
        {
            var obstacles=new List<Bounds>();
            var environment=map==null?stage.Find(_stageLayout.EnvironmentPath):null;
            if (environment!=null)
                foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>())
                { var bounds=renderer.bounds; bounds.Expand(.28f); obstacles.Add(bounds); }
            var resources=map==null?stage.Find(_stageLayout.ResourcesPath):null;
            var flatCells=new HashSet<Vector2Int>();
            var tileColors=new Dictionary<Vector2Int,Color>();
            Texture2D blendMap=null;
            Vector4 blendBounds=Vector4.zero;
            var grid=map==null?stage.Find(_stageLayout.GridPath):null;
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
            var resourcePositions=new List<Vector3>();if(resources!=null)foreach(Transform resource in resources)resourcePositions.Add(resource.position);
            var result=GrassGeometry.Calculate(map,_stageLayout,stage.localToWorldMatrix,forestEnabled,obstacles,resourcePositions,flatCells,tileColors,blendMap,blendBounds);
            var chunks=result.Chunks;foreach(var contact in result.Contacts)_afterGrassContacts[contact.Key]=contact.Value;
            foreach (var entry in chunks)
            {
                var chunk=entry.Value;
                VegetationDensityController.ShuffleCards(chunk.Indices,70421);
                var mesh=new Mesh { name="Batched Grass Chunk", indexFormat=IndexFormat.UInt32 };
                mesh.SetVertices(chunk.Positions); mesh.SetUVs(0,chunk.Uv); mesh.SetUVs(1,chunk.Sizes); mesh.SetUVs(2,chunk.Types);
                mesh.SetColors(chunk.Colors); mesh.SetNormals(chunk.Normals); mesh.SetTriangles(chunk.Indices,0);
                var bounds=mesh.bounds; bounds.Expand(2f); mesh.bounds=bounds;
                var go=new GameObject("BillboardGrass_"+entry.Key.x+"_"+entry.Key.y); go.transform.SetParent(stage,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var rendererOut=go.AddComponent<MeshRenderer>(); rendererOut.sharedMaterial=_material;
                rendererOut.shadowCastingMode=ShadowCastingMode.Off;
                _objects.Add(go); _meshes.Add(mesh);
                _densities.Add(new VegetationDensityController(mesh,rendererOut,chunk.Indices.Count));
            }
            if (forestEnabled && map==null && _stageLayout.DecorationFlower!=null)
                foreach (var decoration in stage.GetComponentsInChildren<Transform>(true))
                {
                    bool? flowers=_stageLayout.DecorationFlower(decoration);
                    if (!flowers.HasValue || !decoration.gameObject.activeSelf) continue;
                    var point=stage.InverseTransformPoint(decoration.position);
                    if (DecorationVisible(flowers.Value,point.x,point.z)) continue;
                    _suppressedDecorations.Add(decoration.gameObject);
                    decoration.gameObject.SetActive(false);
                }
        }

        public bool HasGrassAt(Vector3 worldPoint,Func<Vector3,bool> visible=null)
        {
            int x=Mathf.FloorToInt(worldPoint.x),z=Mathf.FloorToInt(worldPoint.z);
            for (int dzCell=-1;dzCell<=1;dzCell++)
            for (int dxCell=-1;dxCell<=1;dxCell++)
            {
                if (!_afterGrassContacts.TryGetValue(new Vector2Int(x+dxCell,z+dzCell),out var contacts)) continue;
                foreach (var contact in contacts)
                {
                    float dx=worldPoint.x-contact.x, dz=worldPoint.z-contact.y;
                    float radius=(contact.z-.04f)*_sizeScale+.04f;
                    if (dx*dx+dz*dz<=radius*radius && (visible==null || visible(new Vector3(contact.x,worldPoint.y,contact.y)))) return true;
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
            foreach (var go in _objects) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(go);
            foreach (var mesh in _meshes) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(mesh);
            if (_material!=null) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_material);
            _objects.Clear(); _meshes.Clear();
            _densities.Clear();
            _afterGrassContacts.Clear();
        }
    }
}
