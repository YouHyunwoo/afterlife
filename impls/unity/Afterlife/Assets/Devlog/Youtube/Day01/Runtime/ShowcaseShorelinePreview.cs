using System;
using System.Collections.Generic;
using Afterlife.Devlog.Terrain;
using Afterlife.Devlog.Shoreline;
using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    public sealed class ShowcaseShorelinePreview : IDisposable
    {
        private sealed class Tile
        {
            public MeshFilter Filter;
            public Mesh Original;
            public ShowcasePickSurface Surface;
            public ShowcasePickMesh OriginalPick;
            public readonly Mesh[] Meshes=new Mesh[3];
            public readonly ShowcasePickMesh[] Picks=new ShowcasePickMesh[3];
            public readonly Dictionary<Transform,Vector3> Decorations=new Dictionary<Transform,Vector3>();
        }
        private readonly Transform _stage;
        private readonly List<Tile> _tiles=new List<Tile>();
        public ShorelineMode Mode { get; private set; }

        public ShowcaseShorelinePreview(Transform stage)
        {
            _stage=stage;
            var grid=stage.Find("Terrain_Flat/BiomeGrid_12x10");
            if (grid==null) return;
            foreach (Transform tile in grid)
            {
                if (!tile.name.StartsWith("Tile_") || !tile.gameObject.activeSelf || tile.name.EndsWith("_Water")) continue;
                int x=Mathf.FloorToInt(tile.localPosition.x),z=Mathf.FloorToInt(tile.localPosition.z);
                bool coast=false;
                for (int dz=-1;dz<=1;dz++)
                for (int dx=-1;dx<=1;dx++)
                    if (x+dx>=-6 && x+dx<6 && z+dz>=-5 && z+dz<5 && ShowcaseBiomeMap.At(x+dx,z+dz)==ShowcaseBiome.Water) coast=true;
                if (!coast) continue;
                var filter=tile.GetComponent<MeshFilter>(); var surface=tile.GetComponent<ShowcasePickSurface>();
                if (filter==null || filter.sharedMesh==null || !filter.sharedMesh.isReadable) continue;
                var entry=new Tile { Filter=filter,Original=filter.sharedMesh,Surface=surface,OriginalPick=surface!=null ? surface.Mesh : null };
                foreach (Transform child in tile)
                    if (child.name.StartsWith("TileDecoration_")) entry.Decorations.Add(child,child.localPosition);
                _tiles.Add(entry);
            }
        }

        public void Select(ShorelineMode mode)
        {
            Mode=mode;
            foreach (var tile in _tiles)
            {
                foreach (var decoration in tile.Decorations)
                {
                    Vector3 world=tile.Filter.transform.TransformPoint(decoration.Value);
                    Vector3 stagePoint=_stage.InverseTransformPoint(world);
                    world+=_stage.TransformVector(Vector3.up*ShorelineGeometry.Offset(stagePoint,mode));
                    decoration.Key.position=world;
                }
                if (mode==ShorelineMode.Square)
                {
                    tile.Filter.sharedMesh=tile.Original;
                    if (tile.Surface!=null) tile.Surface.Mesh=tile.OriginalPick;
                    continue;
                }
                int slot=(int)mode;
                if (tile.Meshes[slot]==null)
                {
                    var mesh=ShorelineGeometry.Build(tile.Original,_stage.worldToLocalMatrix*tile.Filter.transform.localToWorldMatrix,mode);
                    tile.Meshes[slot]=mesh;
                    var pick=ScriptableObject.CreateInstance<ShowcasePickMesh>(); pick.name=mesh.name+"_Pick";
                    pick.Vertices=mesh.vertices; pick.Normals=mesh.normals; pick.Colors=mesh.colors; pick.Bounds=mesh.bounds;
                    pick.Triangles=mesh.triangles; pick.ColorVersion=2;
                    var materials=new List<int>();
                    for (int sub=0;sub<mesh.subMeshCount;sub++)
                        for (int i=0;i<mesh.GetIndexCount(sub)/3;i++) materials.Add(sub);
                    pick.TriangleMaterials=materials.ToArray(); tile.Picks[slot]=pick;
                }
                tile.Filter.sharedMesh=tile.Meshes[slot];
                if (tile.Surface!=null) tile.Surface.Mesh=tile.Picks[slot];
            }
        }

        public void Dispose()
        {
            Select(ShorelineMode.Square);
            foreach (var tile in _tiles)
                for (int i=1;i<3;i++)
                {
                    if (tile.Meshes[i]!=null) UnityEngine.Object.Destroy(tile.Meshes[i]);
                    if (tile.Picks[i]!=null) UnityEngine.Object.Destroy(tile.Picks[i]);
                }
            _tiles.Clear();
        }
    }
}
