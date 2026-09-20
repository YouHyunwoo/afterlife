using System.Collections.Generic;
using Afterlife.Devlog.Day01;
using Afterlife.Devlog.Terrain;
using Afterlife.Devlog.Wind;
using UnityEngine;

namespace Afterlife.Prototype
{
    // 맵/시야/오브젝트 수명은 프로토타입이 소유하고 표현 모듈에는 필요한 데이터만 준다.
    public sealed partial class PrototypeView
    {
        private Texture2D _groundColors,_groundNormals;
        private Texture _previousGroundColors,_previousGroundNormals;
        private Vector4 _previousGroundBounds;
        private void InitializeGroundAppearance()
        {
            const int resolution=4;
            int size=_model.Terrain.Size*resolution+1;
            float min=-_model.Terrain.Radius-.5f;
            _groundColors=new Texture2D(size,size,TextureFormat.RGBA32,false,false){name="Shared biome colors",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            _groundNormals=new Texture2D(size,size,TextureFormat.RGBA32,false,true){name="Shared ground normals",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[size*size];var normals=new Color[size*size];
            for(int z=0;z<size;z++)for(int x=0;x<size;x++)
            {
                float px=min+x/(float)resolution,pz=min+z/(float)resolution;
                colors[z*size+x]=_terrainSurface.SurfaceColor(px,pz);
                var n=Vector3.up;
                if(_model.Terrain.At(new Point(px,pz))!=TerrainKind.Water)
                    n=new Vector3(_terrainSurface.Height(px-.01f,pz)-_terrainSurface.Height(px+.01f,pz),.02f,
                        _terrainSurface.Height(px,pz-.01f)-_terrainSurface.Height(px,pz+.01f)).normalized;
                normals[z*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            _groundColors.SetPixels(colors);_groundColors.Apply(false,true);
            _groundNormals.SetPixels(normals);_groundNormals.Apply(false,true);
            _previousGroundColors=Shader.GetGlobalTexture("_PrototypeGroundColors");
            _previousGroundNormals=Shader.GetGlobalTexture("_PrototypeGroundNormals");
            _previousGroundBounds=Shader.GetGlobalVector("_PrototypeGroundBounds");
            Shader.SetGlobalTexture("_PrototypeGroundColors",_groundColors);Shader.SetGlobalTexture("_PrototypeGroundNormals",_groundNormals);
            // 경계 격자점까지 텍셀 중심에 대응시킨다.
            Shader.SetGlobalVector("_PrototypeGroundBounds",new Vector4(min-.5f/resolution,min-.5f/resolution,size/(float)resolution,size/(float)resolution));
        }
        private System.Func<Vector3,bool> _grassContactVisible;
        private bool IsGrassContactVisible(Vector3 anchor)
        {
            if(_vegetationMask==null || _vegetationMaskPixels==null)return true;
            int size=_vegetationMask.width;
            float min=-_model.Terrain.Radius-.5f;
            int x=Mathf.Clamp(Mathf.FloorToInt((anchor.x-min)*4),0,size-1);
            int z=Mathf.Clamp(Mathf.FloorToInt((anchor.z-min)*4),0,size-1);
            return _vegetationMaskPixels[z*size+x].r<128;
        }
        private ShowcaseGrass _vegetationGrass;
        private ShowcaseWind _vegetationWind;
        private Shader _leafShader;
        private Texture2D _vegetationMask;
        private Color32[] _vegetationMaskPixels;
        private int _vegetationLayoutHash;
        private bool _vegetationMaskDirty=true;
        private sealed class TreeLeaves
        {
            public ShowcaseLeafCards Cards;
            public int Revision;
            public readonly List<Renderer> Sources=new List<Renderer>();
        }
        private sealed class LeafTint { public TreeLeaves Tree; public Color Color; }
        private sealed class LeafMesh
        { public Vector3[] Vertices,Normals;public int[] Triangles;public Color[] Colors; }
        private readonly Dictionary<int,TreeLeaves> _treeLeaves=new Dictionary<int,TreeLeaves>();
        private readonly Dictionary<Renderer,LeafTint> _leafTints=new Dictionary<Renderer,LeafTint>();
        private readonly Dictionary<Mesh,LeafMesh> _leafMeshes=new Dictionary<Mesh,LeafMesh>();
        private readonly Dictionary<Material,Material> _treeWindMaterials=new Dictionary<Material,Material>();

        private void InitializeVegetation()
        {
            _leafShader=Resources.Load<Shader>("PrototypeLeaves");
            if(_leafShader==null)throw new System.InvalidOperationException("PrototypeLeaves shader missing.");
            _grassContactVisible=IsGrassContactVisible;
            _vegetationWind=new ShowcaseWind();
            _vegetationGrass=new ShowcaseGrass(_mapRoot.transform,new ShowcaseGrass.Map {
                Radius=_model.Terrain.Radius,SamplesPerAxis=7,AccentFraction=.25f,
                Biome=(x,z)=>(ShowcaseBiome)(int)_model.Terrain.At(x,z),
                Height=(x,z)=>_terrainSurface.Height(x,z),
                Color=(x,z)=>_terrainSurface.SurfaceColor(x,z)
            },Resources.Load<Shader>("PrototypeGrass"));
            int size=_model.Terrain.Size*4;
            _vegetationMask=new Texture2D(size,size,TextureFormat.RGBA32,false,true) {
                name="Vegetation footprint mask",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp };
            _vegetationMaskPixels=new Color32[size*size];
            float min=-_model.Terrain.Radius-.5f;
            _vegetationGrass.SetExclusionMask(_vegetationMask,new Vector4(min,min,_model.Terrain.Size,_model.Terrain.Size));
            if(_decorationMaterial!=null)
            {
                _decorationMaterial.SetTexture("_VegetationMask",_vegetationMask);
                _decorationMaterial.SetVector("_VegetationBounds",new Vector4(min,min,_model.Terrain.Size,_model.Terrain.Size));
            }
            RefreshVegetationMask();
        }

        private void AttachTreeLeaves(Body body,Renderer[] renderers)
        {
            if(body.Kind!=Kind.Tree)return;
            var tree=new TreeLeaves();var surfaces=new List<ShowcaseLeafCards.Surface>();
            foreach(var renderer in renderers)
            {
                var filter=renderer.GetComponent<MeshFilter>();
                if(filter==null||filter.sharedMesh==null||filter.sharedMesh==_contactMesh||!filter.sharedMesh.isReadable)continue;
                var mesh=filter.sharedMesh;
                if(!_leafMeshes.TryGetValue(mesh,out var data))
                {
                    data=new LeafMesh {Vertices=mesh.vertices,Normals=mesh.normals,Triangles=mesh.triangles,Colors=mesh.colors};
                    _leafMeshes.Add(mesh,data);
                }
                var tint=renderer.GetComponent<PrototypeTint>().Original;
                var colors=data.Colors;
                if(colors.Length!=data.Vertices.Length)
                {
                    colors=new Color[data.Vertices.Length];
                    for(int i=0;i<colors.Length;i++)colors[i]=tint;
                }
                bool foliage=false;
                foreach(var color in colors)if(color.g>=color.r*1.08f&&color.g>=color.b*1.05f){foliage=true;break;}
                if(!foliage)continue;
                // 원본 모델과 충돌체를 유지하고 초록 수관에만 공통 바람을 적용한다.
                var source=renderer.sharedMaterial;
                if(!_treeWindMaterials.TryGetValue(source,out var windy))
                {
                    windy=new Material(source){name="Prototype tree wind"};windy.SetFloat("_FoliageWind",1);
                    _treeWindMaterials.Add(source,windy);_materials.Add(windy);
                }
                renderer.sharedMaterial=windy;
                surfaces.Add(new ShowcaseLeafCards.Surface {Transform=filter.transform,Vertices=data.Vertices,Normals=data.Normals,
                    Triangles=data.Triangles,Colors=colors,Renderer=renderer});
                tree.Sources.Add(renderer);_leafTints.Add(renderer,new LeafTint {Tree=tree,Color=tint});
            }
            tree.Cards=new ShowcaseLeafCards(surfaces,()=>tree.Revision,_leafShader,384,(source,properties)=> {
                var original=source.GetComponent<PrototypeTint>().Original;
                var current=properties.GetColor("_BaseColor");
                properties.SetColor("_BaseColor",new Color(current.r/Mathf.Max(.001f,original.r),
                    current.g/Mathf.Max(.001f,original.g),current.b/Mathf.Max(.001f,original.b),current.a));
            });
            tree.Cards.Tick(_camera);_treeLeaves.Add(body.Id,tree);
        }
        private void NotifyVegetationTint(Renderer renderer,Color color)
        {
            if(_leafTints.TryGetValue(renderer,out var state)&&state.Color!=color)
            {state.Color=color;state.Tree.Revision++;}
        }
        private void RemoveTreeLeaves(int id)
        {
            if(!_treeLeaves.TryGetValue(id,out var tree))return;
            tree.Cards.Dispose();foreach(var source in tree.Sources)_leafTints.Remove(source);_treeLeaves.Remove(id);
        }
        private void UpdateGrassPalette()
        {
            if(_vegetationGrass==null)return;
            _vegetationGrass.SetSizeScale(_settings.GrassSizeScale);
            _vegetationGrass.SetFloat("_GrassUsesTerrainColor",_settings.GrassUsesTerrainColor?1:0);
            _vegetationGrass.SetColor("_GrassBaseColor",_settings.GrassBaseColor);
            _vegetationGrass.SetColor("_GrassBeigeColor",_settings.GrassBeigeColor);
            _vegetationGrass.SetColor("_GrassDarkColor",_settings.GrassDarkColor);
            _vegetationGrass.SetColor("_GrassPaleColor",_settings.GrassPaleColor);
            _vegetationGrass.SetColor("_GrassTealColor",_settings.GrassTealColor);
            _vegetationGrass.SetColor("_GrassMossColor",_settings.GrassMossColor);
            _vegetationGrass.SetColor("_GrassPurpleColor",_settings.GrassPurpleColor);
            _vegetationGrass.SetColor("_GrassFlowerPink",_settings.GrassFlowerPink);
            _vegetationGrass.SetColor("_GrassFlowerYellow",_settings.GrassFlowerYellow);
        }
        private void UpdateVegetation()
        {
            _vegetationWind?.Tick(_effectTime);
            UpdateGrassPalette();
            _vegetationGrass?.Tick(_camera);
            foreach(var tree in _treeLeaves.Values)tree.Cards.Tick(_camera);
            RefreshVegetationMask();
        }
        private void RefreshVegetationMask()
        {
            if(_vegetationMask==null)return;
            int hash=17;
            foreach(var b in _model.Bodies)
                if(b.Alive&&(b.Building||b.Kind==Kind.Tree||b.Kind==Kind.Rock))
                    unchecked {hash=hash*31+b.Id;hash=hash*31+b.Position.X.GetHashCode();hash=hash*31+b.Position.Z.GetHashCode();hash=hash*31+b.Width;hash=hash*31+b.Depth;}
            if(!_vegetationMaskDirty&&hash==_vegetationLayoutHash)return;
            _vegetationLayoutHash=hash;_vegetationMaskDirty=false;
            System.Array.Clear(_vegetationMaskPixels,0,_vegetationMaskPixels.Length);
            int size=_vegetationMask.width;float min=-_model.Terrain.Radius-.5f;
            foreach(var b in _model.Bodies)
            {
                if(!b.Alive||!(b.Building||b.Kind==Kind.Tree||b.Kind==Kind.Rock))continue;
                float halfX=b.Building?b.Width*.5f+.12f:.30f,halfZ=b.Building?b.Depth*.5f+.12f:.30f;
                int x0=Mathf.Clamp(Mathf.FloorToInt((b.Position.X-halfX-min)*4),0,size-1),x1=Mathf.Clamp(Mathf.CeilToInt((b.Position.X+halfX-min)*4),0,size);
                int z0=Mathf.Clamp(Mathf.FloorToInt((b.Position.Z-halfZ-min)*4),0,size-1),z1=Mathf.Clamp(Mathf.CeilToInt((b.Position.Z+halfZ-min)*4),0,size);
                for(int z=z0;z<z1;z++)for(int x=x0;x<x1;x++)_vegetationMaskPixels[z*size+x]=new Color32(255,0,0,255);
            }
            _vegetationMask.SetPixels32(_vegetationMaskPixels);_vegetationMask.Apply(false,false);
        }
        private void ResetVegetation()
        {
            foreach(var tree in _treeLeaves.Values)tree.Cards.Dispose();
            _treeLeaves.Clear();_leafTints.Clear();_vegetationMaskDirty=true;
        }
        private void DisposeVegetation()
        {
            ResetVegetation();_vegetationGrass?.Dispose();_vegetationGrass=null;
            _vegetationWind?.Dispose();_vegetationWind=null;
            if(_vegetationMask!=null)Destroy(_vegetationMask);
            foreach(var material in _treeWindMaterials.Values){_materials.Remove(material);Destroy(material);}
            _leafMeshes.Clear();_treeWindMaterials.Clear();
            if(_groundColors!=null)
            {
                Shader.SetGlobalTexture("_PrototypeGroundColors",_previousGroundColors);
                Shader.SetGlobalTexture("_PrototypeGroundNormals",_previousGroundNormals);
                Shader.SetGlobalVector("_PrototypeGroundBounds",_previousGroundBounds);
                Destroy(_groundColors);Destroy(_groundNormals);_groundColors=null;_groundNormals=null;
            }
        }
    }
}
