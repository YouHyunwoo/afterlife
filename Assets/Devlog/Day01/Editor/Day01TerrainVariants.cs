using System.Collections.Generic;
using System.Linq;
using Afterlife.Devlog.Terrain;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    public static class Day01TerrainVariants
    {
        private const string Root=Day01SceneBuilder.Root;
        private static readonly Color[] Palette={new Color(.30f,.40f,.23f),new Color(.43f,.34f,.25f),new Color(.68f,.61f,.44f),
            new Color(.1f,.4f,.6f),new Color(.81f,.85f,.88f),new Color(.23f,.32f,.19f),new Color(.43f,.45f,.43f)};

        public static void ApplyTo(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion>=9) return;
            for (int phase=1;phase<showcase.Stages.Length;phase++)
            {
                var stage=showcase.Stages[phase];
                var environment=stage.Find("Environment").GetComponentsInChildren<Renderer>();
                var resources=stage.GetComponentsInChildren<ShowcaseTarget>(true);
                var grid=stage.Find("Terrain_Flat/BiomeGrid_12x10");
                var tiles=new Dictionary<Vector2Int,Transform>();
                foreach (Transform tile in grid)
                {
                    if (!tile.name.StartsWith("Tile_") || !tile.gameObject.activeSelf || tile.name.EndsWith("_Water")) continue;
                    int x=Mathf.FloorToInt(tile.localPosition.x),z=Mathf.FloorToInt(tile.localPosition.z);
                    var biome=ShowcaseBiomeMap.At(x,z); int shape=ShowcaseTileVariants.Shape(x,z);
                    Vector3 world=stage.TransformPoint(new Vector3(x+.5f,0,z+.5f));
                    bool flat=environment.Any(r=>Overlap(r.bounds,world,.62f));
                    Undo.RecordObjects(new UnityEngine.Object[]{tile,tile.GetComponent<MeshFilter>(),tile.GetComponent<MeshRenderer>()},"지형 타일 변형");
                    tile.localPosition=new Vector3(x+.5f,0,z+.5f); tile.localScale=Vector3.one;
                    tile.localRotation=Quaternion.Euler(0,ShowcaseTileVariants.Rotation(x,z)*90,0);
                    tile.GetComponent<MeshFilter>().sharedMesh=TerrainMesh(biome,shape,phase,flat);
                    var old=tile.GetComponent<MeshRenderer>().sharedMaterials;
                    var side=old.Length>1 ? old[1] : TopMaterial(biome);
                    tile.GetComponent<MeshRenderer>().sharedMaterials=new[]{TopMaterial(biome),side};
                    tile.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                    tiles.Add(new Vector2Int(x,z),tile);
                    foreach (var oldDecoration in tile.Cast<Transform>().Where(t=>t.name.StartsWith("TileDecoration_")).ToArray())
                        Undo.DestroyObjectImmediate(oldDecoration.gameObject);
                    var variant=ShowcaseTileVariants.Decoration(biome,x,z);
                    if (!flat && variant!=TileDecoration.Empty)
                        AddDecoration(tile,stage,biome,shape,variant,x,z,resources,environment);
                }
                var oldDetails=stage.Find("BiomeDetails");
                if (oldDetails!=null) { Undo.RecordObject(oldDetails.gameObject,"기존 장식 보존"); oldDetails.gameObject.SetActive(false); }
                foreach (var target in resources)
                {
                    Vector3 p=stage.InverseTransformPoint(target.transform.position);
                    int x=Mathf.FloorToInt(p.x),z=Mathf.FloorToInt(p.z);
                    if (!tiles.TryGetValue(new Vector2Int(x,z),out var tile)) continue;
                    bool flat=tile.GetComponent<MeshFilter>().sharedMesh.name.EndsWith("_Flat");
                    p.y=flat ? 0 : (float)ShowcaseTileVariants.Height(x,z,p.x-x,p.z-z);
                    Undo.RecordObject(target.transform,"자원 바닥 높이 정렬"); target.transform.position=stage.TransformPoint(p);
                }
            }
            Undo.RecordObject(showcase,"타일 변형 버전 기록"); showcase.LayoutVersion=9;
            AssetDatabase.SaveAssets();
        }
        private static bool Overlap(Bounds b,Vector3 p,float radius) => p.x+radius>b.min.x && p.x-radius<b.max.x && p.z+radius>b.min.z && p.z-radius<b.max.z;

        private static Material TopMaterial(ShowcaseBiome biome)
        {
            string path=Root+"/Materials/TerrainVariant_"+biome+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material!=null)
            {
                Undo.RecordObject(material,"지형 명암 단계 완화"); material.SetFloat("_Stylized",.25f);
                EditorUtility.SetDirty(material); return material;
            }
            material=new Material(Shader.Find("Afterlife/Devlog/ShowcaseSurface")) { name="TerrainVariant_"+biome };
            material.SetColor("_BaseColor",Palette[(int)biome]);
            material.SetFloat("_VertexPalette",1); material.SetFloat("_Ground",0);
            material.SetFloat("_Grid",.22f); material.SetFloat("_Stylized",.25f);
            AssetDatabase.CreateAsset(material,path); return material;
        }

        private static Mesh TerrainMesh(ShowcaseBiome biome,int shape,int phase,bool flat)
        {
            string name=$"TerrainV2_{phase}_{biome}_{(flat ? 0 : shape)}"+(flat ? "_Flat" : "_Relief");
            string path=Root+"/Geometry/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh!=null) return mesh;
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var colors=new List<Color>();
            var top=new List<int>(); var sides=new List<int>();
            void Tri(Vector3 a,Vector3 b,Vector3 c,Color color,List<int> indices)
            {
                int first=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                Vector3 n=Vector3.Cross(b-a,c-a).normalized;
                for (int i=0;i<3;i++) { normals.Add(n); colors.Add(color); indices.Add(first+i); }
            }
            Vector3 Node(int x,int z) => new Vector3((float)ShowcaseTileVariants.NodeU(shape,x,z)-.5f,flat ? 0 : (float)ShowcaseTileVariants.NodeHeight(biome,shape,x,z),(float)ShowcaseTileVariants.NodeV(shape,x,z)-.5f);
            for (int z=0;z<4;z++)
            for (int x=0;x<4;x++)
            {
                if (ShowcaseTileVariants.AlternateDiagonal(shape,x,z))
                {
                    Tri(Node(x,z),Node(x,z+1),Node(x+1,z),Color.white,top);
                    Tri(Node(x+1,z),Node(x,z+1),Node(x+1,z+1),Color.white,top);
                }
                else
                {
                    Tri(Node(x,z),Node(x,z+1),Node(x+1,z+1),Color.white,top);
                    Tri(Node(x,z),Node(x+1,z+1),Node(x+1,z),Color.white,top);
                }
            }
            float bottom=phase==2 ? -.8f : -.04f;
            var corners=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};
            for (int i=0;i<4;i++)
            {
                Vector3 a=corners[i],b=corners[(i+1)%4],c=b+Vector3.up*bottom,d=a+Vector3.up*bottom;
                Tri(b,a,d,Color.white,sides); Tri(b,d,c,Color.white,sides);
            }
            Tri(corners[0]+Vector3.up*bottom,corners[2]+Vector3.up*bottom,corners[1]+Vector3.up*bottom,Color.white,sides);
            Tri(corners[0]+Vector3.up*bottom,corners[3]+Vector3.up*bottom,corners[2]+Vector3.up*bottom,Color.white,sides);
            mesh=new Mesh { name=name }; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors);
            mesh.subMeshCount=2; mesh.SetTriangles(top,0); mesh.SetTriangles(sides,1); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path); return mesh;
        }

        private static void AddDecoration(Transform tile,Transform stage,ShowcaseBiome biome,int shape,TileDecoration variant,int x,int z,
            ShowcaseTarget[] resources,Renderer[] environment)
        {
            var random=new System.Random(ShowcaseTileVariants.Seed(x,z));
            string[] assets=variant==TileDecoration.MossyRock ? new[]{"Rock0","Fern"} : variant==TileDecoration.Flowers ? new[]{"Flowers","Flowers"}
                : variant==TileDecoration.Ferns ? new[]{"Fern","Fern"} : new[]{"Pebbles"};
            for (int i=0;i<assets.Length;i++)
            {
                float u=.25f+(float)random.NextDouble()*.5f,v=.25f+(float)random.NextDouble()*.5f;
                Vector3 p=new Vector3(u-.5f,(float)ShowcaseTileVariants.LocalHeight(biome,shape,u,v),v-.5f);
                Vector3 world=tile.TransformPoint(p);
                if (environment.Any(r=>Overlap(r.bounds,world,.25f)) || resources.Any(r=>{
                    Vector3 d=r.transform.position-world; d.y=0; return d.sqrMagnitude<.6f*.6f; })) continue;
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/"+assets[i]+".fbx");
                if (asset==null) continue;
                var pivot=new GameObject("TileDecoration_"+variant+"_"+i).transform;
                Undo.RegisterCreatedObjectUndo(pivot.gameObject,"타일 종속 장식 생성"); pivot.SetParent(tile,false);
                pivot.localPosition=p; pivot.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                pivot.localScale=Vector3.one*(assets[i]=="Rock0" ? .42f : .45f+(float)random.NextDouble()*.2f);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,pivot);
                Undo.RegisterCreatedObjectUndo(instance,"기존 모델로 타일 장식 배치");
                var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+(assets[i]=="Fern" || assets[i]=="Flowers" ? "AfterFoliage" : "AfterModels")+".mat");
                if (variant==TileDecoration.DirtClods)
                {
                    string path=Root+"/Materials/DirtClods.mat";
                    material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material==null)
                    {
                        material=new Material(Shader.Find("Afterlife/Devlog/ShowcaseSurface")) { name="DirtClods" };
                        material.SetColor("_BaseColor",new Color(.34f,.255f,.18f)); material.SetFloat("_VertexPalette",0);
                        material.SetFloat("_Stylized",.35f); AssetDatabase.CreateAsset(material,path);
                    }
                    pivot.localScale=new Vector3(.8f,.55f,.7f);
                }
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                { renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray(); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
            }
        }
    }
}
