using Afterlife.Modules.Interaction;
using System;
using System.Collections.Generic;
using Afterlife.Modules.Hover;
using Afterlife.Devlog.Shoreline;
using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    // 모래와 눈의 GPU 변위용 메시와 CPU 판정을 준비한다. 물은 원본 메시를 사용한다.
    public sealed class ShowcaseHoverTerrain : IDisposable
    {
        private sealed class Entry
        {
            public MeshFilter Filter;
            public Renderer Renderer;
            public Mesh Original, Dense;
            public PickSurface Surface;
            public PickMeshData OriginalPick, Pick;
        }
        private readonly List<Entry> _entries=new List<Entry>();
        public ShowcaseHoverTerrain(Transform stage,TerrainHoverEffects hover)
        {
            var grid=stage.Find("Terrain_Flat/BiomeGrid_12x10");
            if (grid==null) return;
            foreach (Transform tile in grid)
            {
                // 픽셀 물결은 표면 높이를 바꾸지 않으므로 세분화나 높이 콜백이 필요 없다.
                if (!tile.gameObject.activeSelf || !(tile.name.EndsWith("_Sand") || tile.name.EndsWith("_Snow"))) continue;
                var filter=tile.GetComponent<MeshFilter>(); var surface=tile.GetComponent<PickSurface>();
                if (filter==null || surface==null || !filter.sharedMesh.isReadable) continue;
                var dense=filter.sharedMesh.vertexCount>2000 ? UnityEngine.Object.Instantiate(filter.sharedMesh)
                    : ShorelineGeometry.Build(filter.sharedMesh,stage.worldToLocalMatrix*tile.localToWorldMatrix,ShorelineMode.Square,(_,_)=>false,4);
                var pick=ScriptableObject.CreateInstance<PickMeshData>();
                pick.Vertices=dense.vertices; pick.Normals=dense.normals; pick.Triangles=dense.triangles;
                pick.Colors=dense.colors; pick.Bounds=dense.bounds; pick.ColorVersion=2;
                var slots=new List<int>();
                for (int sub=0;sub<dense.subMeshCount;sub++) for (int i=0;i<dense.GetIndexCount(sub)/3;i++) slots.Add(sub);
                pick.TriangleMaterials=slots.ToArray();
                var renderer=tile.GetComponent<Renderer>();
                _entries.Add(new Entry { Filter=filter,Renderer=renderer,Original=filter.sharedMesh,Dense=dense,
                    Surface=surface,OriginalPick=surface.Mesh,Pick=pick });
                filter.sharedMesh=dense; surface.Mesh=pick; surface.HeightOffset=hover.HeightOffset;
                var props=new MaterialPropertyBlock(); renderer.GetPropertyBlock(props); props.SetFloat("_HoverTerrain",1); renderer.SetPropertyBlock(props);
                var bounds=dense.bounds; bounds.Expand(.25f); dense.bounds=bounds;
            }
        }
        public void Dispose()
        {
            foreach (var entry in _entries)
            {
                entry.Filter.sharedMesh=entry.Original;
                entry.Surface.Mesh=entry.OriginalPick; entry.Surface.HeightOffset=null;
                var props=new MaterialPropertyBlock(); entry.Renderer.GetPropertyBlock(props); props.SetFloat("_HoverTerrain",0); entry.Renderer.SetPropertyBlock(props);
                UnityEngine.Object.Destroy(entry.Dense); UnityEngine.Object.Destroy(entry.Pick);
            }
            _entries.Clear();
        }
    }
}
