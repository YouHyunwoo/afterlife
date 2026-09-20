using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        // Decoration derives from the authoritative tile map. Per-chunk combined meshes avoid thousands of objects.
        private void BuildTerrainDetails()
        {
            var samples=new Dictionary<string,GameObject>();
            foreach(string name in new[]{"Fern","Reeds","Pebbles","FallenLog","Flowers"})
            {
                var source=Resources.Load<GameObject>("AfterlifePrototype/World/"+name);
                if(source==null)continue;var sample=Instantiate(source);sample.transform.position=Vector3.zero;samples[name]=sample;sample.SetActive(false);
            }
            var random=new System.Random(1786);
            var chunks=new Dictionary<Vector2Int,List<CombineInstance>>();
            for(int z=-_settings.MapRadius+1;z<_settings.MapRadius;z++)for(int x=-_settings.MapRadius+1;x<_settings.MapRadius;x++)
            {
                if(Mathf.Abs(x)<4&&Mathf.Abs(z)<4)continue;
                var kind=_model.Terrain.At(x,z);if(kind==TerrainKind.Water)continue;
                // 타일마다 같은 밀도로 흩뿌리지 않고 넓은 군집과 빈 공간을 만든다.
                float cluster=Mathf.PerlinNoise(x*.19f+51,z*.19f+73);
                string key=null;double chance=random.NextDouble()/Mathf.Lerp(.45f,1.4f,cluster);
                bool shore=_model.Terrain.At(x+1,z)==TerrainKind.Water||_model.Terrain.At(x-1,z)==TerrainKind.Water||_model.Terrain.At(x,z+1)==TerrainKind.Water||_model.Terrain.At(x,z-1)==TerrainKind.Water;
                if(shore&&chance<.42)key="Reeds";
                else if(kind==TerrainKind.Forest&&chance<.52)key=chance<.035?"FallenLog":"Fern";
                else if(kind==TerrainKind.Rocky&&chance<.58||kind==TerrainKind.Sand&&chance<.18||kind==TerrainKind.Dirt&&chance<.12)key="Pebbles";
                else if(kind==TerrainKind.Grass&&chance<.065)key="Flowers";
                if(key==null||!samples.TryGetValue(key,out var sample))continue;
                var coord=new Vector2Int(Mathf.FloorToInt(x/8f),Mathf.FloorToInt(z/8f));
                if(!chunks.TryGetValue(coord,out var list)){list=new List<CombineInstance>();chunks.Add(coord,list);}
                var pos=new Vector3(x+(float)random.NextDouble()*.9f-.45f,-.045f,z+(float)random.NextDouble()*.9f-.45f);
                pos.y+=GroundLift(pos.x,pos.z);
                var matrix=Matrix4x4.TRS(pos,Quaternion.Euler(0,(float)random.NextDouble()*360,0),Vector3.one*(.8f+(float)random.NextDouble()*.35f));
                foreach(var filter in sample.GetComponentsInChildren<MeshFilter>(true))list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=matrix*filter.transform.localToWorldMatrix});
            }
            var material=new Material(_surface){name="Batched biome palette"};material.SetFloat("_VertexPalette",1);material.SetFloat("_ZWrite",1);_materials.Add(material);
            foreach(var pair in chunks)
            {
                var mesh=new Mesh{name="Biome patch "+pair.Key,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);_environmentMeshes.Add(mesh);
                var go=new GameObject(mesh.name);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;Tint(renderer,Color.white);
            }
            foreach(var sample in samples.Values)Destroy(sample);
        }
    }
}
