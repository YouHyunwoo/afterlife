using Afterlife.Modules.Vegetation;
using System.Collections.Generic;
using Afterlife.Modules.Terrain;
using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private Material _decorationMaterial;
        private readonly List<Mesh> _decorationMeshes=new List<Mesh>();
        private sealed class DecorationSource
        {
            public Matrix4x4 Transform;
            public Vector3[] Vertices,Normals;
            public Color[] Colors;
            public int[] Triangles;
        }
        private sealed class DecorationChunk
        {
            public readonly List<Vector3> Vertices=new List<Vector3>(),Normals=new List<Vector3>();
            public readonly List<Color> Colors=new List<Color>();
            public readonly List<Vector2> Ground=new List<Vector2>(),Anchors=new List<Vector2>();
            public readonly List<int> Triangles=new List<int>();
        }
        private void BuildTerrainDetails()
        {
            var samples=new Dictionary<string,List<DecorationSource>>();
            foreach(string name in new[]{"Fern","Pebbles","Rock0","Flowers"})
            {
                var source=WorldAsset(name);
                if(source==null){Debug.LogWarning("Missing terrain decoration: "+name);continue;}
                var sample=Instantiate(source);sample.transform.position=Vector3.zero;sample.SetActive(false);
                var parts=new List<DecorationSource>();
                try
                {
                    foreach(var filter in sample.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var mesh=filter.sharedMesh;if(mesh==null||!mesh.isReadable)continue;
                        parts.Add(new DecorationSource {Transform=filter.transform.localToWorldMatrix,
                            Vertices=mesh.vertices,Normals=mesh.normals,Colors=mesh.colors,Triangles=mesh.triangles});
                    }
                }
                finally { Destroy(sample); }
                samples[name]=parts;
            }
            var chunks=new Dictionary<Vector2Int,DecorationChunk>();
            for(int z=-_model.Terrain.Radius;z<=_model.Terrain.Radius;z++)
            for(int x=-_model.Terrain.Radius;x<=_model.Terrain.Radius;x++)
            {
                var biome=(TerrainBiome)(int)_model.Terrain.At(x,z);
                var variant=TerrainTileVariants.Decoration(biome,x,z);
                if(variant==TileDecoration.Empty)continue;
                // 장식은 1/4만 유지한다. 초원/숲의 바탕 풀은 별도 생성되어 그대로 남는다.
                if(TerrainTileVariants.Seed(x+137,z-271)%4!=0)continue;
                string[] assets=variant==TileDecoration.MossyRock?new[]{"Rock0","Fern"}:
                    variant==TileDecoration.Flowers?new[]{"Flowers","Flowers"}:variant==TileDecoration.Ferns?new[]{"Fern","Fern"}:new[]{"Pebbles"};
                var random=new System.Random(TerrainTileVariants.Seed(x,z));
                foreach(string asset in assets)
                {
                    float px=x-.25f+(float)random.NextDouble()*.5f,pz=z-.25f+(float)random.NextDouble()*.5f;
                    float yaw=(float)random.NextDouble()*360;
                    float scale=asset=="Rock0"?.42f:.45f+(float)random.NextDouble()*.2f;
                    if((variant==TileDecoration.Flowers || variant==TileDecoration.Ferns)
                        && !GrassRenderer.DecorationVisible(variant==TileDecoration.Flowers,px,pz))continue;
                    if(!samples.TryGetValue(asset,out var sample))continue;
                    var coord=new Vector2Int(Mathf.FloorToInt(x/8f),Mathf.FloorToInt(z/8f));
                    if(!chunks.TryGetValue(coord,out var chunk)){chunk=new DecorationChunk();chunks.Add(coord,chunk);}
                    // 타일 중심 규약과 현재 지형 높이를 사용한다. FBX의 원래 축/스케일은 보존한다.
                    var anchor=new Vector3(px,_terrainSurface.Height(px,pz)+.007f,pz);
                    var placement=Matrix4x4.TRS(anchor,Quaternion.Euler(0,yaw,0),variant==TileDecoration.DirtClods?new Vector3(.8f,.55f,.7f):Vector3.one*scale);
                    foreach(var part in sample)
                    {
                        var matrix=placement*part.Transform;
                        var normalMatrix=matrix.inverse.transpose;
                        var vertices=part.Vertices;var normals=part.Normals;var colors=part.Colors;
                        int first=chunk.Vertices.Count;
                        for(int i=0;i<vertices.Length;i++)
                        {
                            var color=i<colors.Length?colors[i]:Color.white;
                            if(variant==TileDecoration.DirtClods)color=new Color(.34f,.255f,.18f);
                            bool plant=color.g>color.r*1.08f && color.g>color.b*1.05f;
                            chunk.Vertices.Add(matrix.MultiplyPoint3x4(vertices[i]));
                            chunk.Normals.Add(normalMatrix.MultiplyVector(i<normals.Length?normals[i]:Vector3.up).normalized);
                            chunk.Colors.Add(color);chunk.Ground.Add(new Vector2(plant?1:0,0));chunk.Anchors.Add(new Vector2(px,pz));
                        }
                        foreach(int index in part.Triangles)chunk.Triangles.Add(first+index);
                    }
                }
            }
            _decorationMaterial=new Material(_surface){name="Day01 biome decorations"};
            _decorationMaterial.SetFloat("_VertexPalette",1);_decorationMaterial.SetFloat("_GroundDecoration",1);_decorationMaterial.SetFloat("_ZWrite",1);_materials.Add(_decorationMaterial);
            foreach(var pair in chunks)
            {
                var chunk=pair.Value;var mesh=new Mesh{name="Biome decorations "+pair.Key,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
                mesh.SetVertices(chunk.Vertices);mesh.SetNormals(chunk.Normals);mesh.SetColors(chunk.Colors);
                mesh.SetUVs(2,chunk.Ground);mesh.SetUVs(3,chunk.Anchors);mesh.SetTriangles(chunk.Triangles,0);mesh.RecalculateBounds();_environmentMeshes.Add(mesh);_decorationMeshes.Add(mesh);
                var go=new GameObject(mesh.name);go.transform.SetParent(_mapRoot.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=_decorationMaterial;Tint(renderer,Color.white);
            }
        }
    }
}
