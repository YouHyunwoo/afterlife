using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Afterlife.Devlog.Day01.Editor
{
    // After만 갱신한다. 육지 윗면은 기존 y=0에 유지한다.
    public static class Day01WaterUpgrade
    {
        private const string Root = Day01SceneBuilder.Root;
        private const float LandHeight = .8f, BedHeight = LandHeight*.05f, WaterHeight = LandHeight*.45f;

        public static void ApplyTo(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion >= 3) return;
            if (showcase.LayoutVersion != 2) throw new InvalidOperationException("Biome grid must be installed before water.");
            var shader = Shader.Find("Afterlife/Devlog/ShowcaseWater");
            if (shader == null) throw new InvalidOperationException("ShowcaseWater shader has not imported yet.");
            EnsureFolder(Root + "/Geometry"); EnsureFolder(Root + "/Rendering");
            var grid = showcase.Stages[2].Find("Terrain_Flat/BiomeGrid_12x10");
            if (grid == null) throw new InvalidOperationException("After biome grid is missing.");
            var tiles = grid.Cast<Transform>().Where(t => t.name.StartsWith("Tile_") && t.gameObject.activeSelf).ToArray();
            var allCells = new HashSet<Vector2Int>();
            var waterCells = new HashSet<Vector2Int>();
            foreach (var tile in tiles)
            {
                var cell = new Vector2Int(Mathf.FloorToInt(tile.localPosition.x),Mathf.FloorToInt(tile.localPosition.z));
                allCells.Add(cell);
                if (tile.name.EndsWith("_Water")) waterCells.Add(cell);
            }
            var block = UnitBlock();
            var soil = SideMaterial("BlockSoil",new Color(.31f,.22f,.14f));
            var sand = SideMaterial("BlockSand",new Color(.58f,.47f,.29f));
            var rock = SideMaterial("BlockRock",new Color(.29f,.32f,.34f));
            foreach (var tile in tiles)
            {
                if (tile.name.EndsWith("_Water"))
                { Undo.RecordObject(tile.gameObject,"기존 수면 보존"); tile.gameObject.SetActive(false); continue; }
                var filter = tile.GetComponent<MeshFilter>(); var renderer = tile.GetComponent<MeshRenderer>();
                Undo.RecordObjects(new UnityEngine.Object[]{tile,filter,renderer},"After 타일을 블록으로 변경");
                Material top = renderer.sharedMaterial;
                Material side = tile.name.EndsWith("_Sand") ? sand : tile.name.EndsWith("_Rocky") || tile.name.EndsWith("_Snow") ? rock : soil;
                filter.sharedMesh = block;
                tile.localPosition += Vector3.down*LandHeight;
                tile.localScale = new Vector3(tile.localScale.x*10,LandHeight,tile.localScale.z*10);
                renderer.sharedMaterials = new[]{top,side};
            }
            var floor = Group(grid,"OceanFloor");
            var floorTop = AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/After_Tile_Sand.mat");
            var waterMesh = new MeshDraft();
            var offsets = new[]{Vector2Int.left,Vector2Int.right,Vector2Int.down,Vector2Int.up};
            foreach (var cell in waterCells.OrderBy(c => c.y).ThenBy(c => c.x))
            {
                float bedBottom = -LandHeight, bottom = bedBottom+BedHeight, top = bottom+WaterHeight, bedTop = bottom;
                Vector3 center = new Vector3(cell.x+.5f,0,cell.y+.5f);
                var bed = Group(floor,$"Seabed_{cell.x+6:00}_{cell.y+5:00}");
                bed.localPosition = center + Vector3.up*bedBottom;
                bed.localScale = new Vector3(1,BedHeight,1);
                bed.gameObject.AddComponent<MeshFilter>().sharedMesh = block;
                bed.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[]{floorTop,sand};
                AddSubmergedPebbles(floor,center+Vector3.up*bedTop,cell);
                var shore = new Vector4(
                    Shore(cell+offsets[0],allCells,waterCells),Shore(cell+offsets[1],allCells,waterCells),
                    Shore(cell+offsets[2],allCells,waterCells),Shore(cell+offsets[3],allCells,waterCells));
                Vector3 a = center+new Vector3(-.5f,top,-.5f), b = center+new Vector3(-.5f,top,.5f);
                Vector3 c = center+new Vector3(.5f,top,.5f), d = center+new Vector3(.5f,top,-.5f);
                waterMesh.Quad(a,b,c,d,shore);
                // 물끼리 맞닿는 면은 생성하지 않아 투명 면의 중첩을 막는다.
                if (!waterCells.Contains(cell+Vector2Int.left)) waterMesh.Quad(new Vector3(a.x,bottom,a.z),new Vector3(b.x,bottom,b.z),b,a,Vector4.zero);
                if (!waterCells.Contains(cell+Vector2Int.right)) waterMesh.Quad(new Vector3(c.x,bottom,c.z),new Vector3(d.x,bottom,d.z),d,c,Vector4.zero);
                if (!waterCells.Contains(cell+Vector2Int.down)) waterMesh.Quad(new Vector3(d.x,bottom,d.z),new Vector3(a.x,bottom,a.z),a,d,Vector4.zero);
                if (!waterCells.Contains(cell+Vector2Int.up)) waterMesh.Quad(new Vector3(b.x,bottom,b.z),new Vector3(c.x,bottom,c.z),c,b,Vector4.zero);
            }
            string waterPath = Root+"/Geometry/OceanBlocks.asset";
            Mesh oceanMesh = AssetDatabase.LoadAssetAtPath<Mesh>(waterPath);
            if (oceanMesh == null) { oceanMesh = waterMesh.ToMesh("OceanBlocks"); AssetDatabase.CreateAsset(oceanMesh,waterPath); }
            var surface = Group(grid,"OceanBlocks_HalfHeight");
            surface.gameObject.AddComponent<MeshFilter>().sharedMesh = oceanMesh;
            var waterRenderer = surface.gameObject.AddComponent<MeshRenderer>();
            waterRenderer.sharedMaterial = WaterMaterial(shader);
            waterRenderer.shadowCastingMode = ShadowCastingMode.Off;
            waterRenderer.receiveShadows = false;
            ConfigureDepthPipeline(showcase);
            Undo.RecordObject(showcase,"블록 수면 버전 기록"); showcase.LayoutVersion = 3;
            AssetDatabase.SaveAssets();
            Debug.Log($"Devlog After: {tiles.Length-waterCells.Count} land blocks (0.8), {waterCells.Count} water cells (0.36) on seabed blocks (0.04).",showcase);
        }

        public static void ApplySeparatedLayers(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion >= 6) return;
            var grid = showcase.Stages[2].Find("Terrain_Flat/BiomeGrid_12x10");
            var floor = grid.Find("OceanFloor");
            foreach (Transform child in floor)
            {
                Undo.RecordObject(child,"해저 5% 높이 배치");
                Vector3 position = child.localPosition;
                if (child.name.StartsWith("Seabed_"))
                {
                    position.y = -LandHeight;
                    child.localScale = new Vector3(child.localScale.x,BedHeight,child.localScale.z);
                }
                else if (child.name.StartsWith("SubmergedPebbles_")) position.y = -LandHeight+BedHeight;
                child.localPosition = position;
            }
            var mesh = grid.Find("OceanBlocks_HalfHeight").GetComponent<MeshFilter>().sharedMesh;
            Undo.RecordObject(mesh,"물 블록 바닥을 해저 윗면에 정렬");
            Vector3[] vertices = mesh.vertices;
            for (int i=0;i<vertices.Length;i++)
                if (vertices[i].y < -LandHeight+BedHeight) vertices[i].y = -LandHeight+BedHeight;
            mesh.vertices = vertices; mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            var material = grid.Find("OceanBlocks_HalfHeight").GetComponent<MeshRenderer>().sharedMaterial;
            Undo.RecordObject(material,"수심 계산 바닥 정렬");
            material.SetFloat("_BottomY",-LandHeight+BedHeight);
            EditorUtility.SetDirty(material);
            Undo.RecordObject(showcase,"물/해저 분리 버전 기록");
            showcase.LayoutVersion = 6;
            AssetDatabase.SaveAssets();
        }

        private static float Shore(Vector2Int cell, HashSet<Vector2Int> all, HashSet<Vector2Int> water) => all.Contains(cell) && !water.Contains(cell) ? 1 : 0;

        private static void AddSubmergedPebbles(Transform parent, Vector3 position, Vector2Int cell)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/Pebbles.fbx");
            if (asset == null) return;
            var pivot = Group(parent,$"SubmergedPebbles_{cell.x+6:00}_{cell.y+5:00}");
            pivot.localPosition = position;
            pivot.localRotation = Quaternion.Euler(0,cell.x*47+cell.y*31,0);
            pivot.localScale = Vector3.one*1.1f;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset,pivot);
            Undo.RegisterCreatedObjectUndo(instance,"물 아래 기존 자갈 배치");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/AfterModels.mat");
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static void ConfigureDepthPipeline(Day01Showcase showcase)
        {
            string rendererPath = Root+"/Rendering/ShowcaseDepthRenderer.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                var source = showcase.Pipeline.rendererDataList[0] as UniversalRendererData;
                if (source == null) throw new InvalidOperationException("A Universal Renderer is required for water depth.");
                renderer = UnityEngine.Object.Instantiate(source); renderer.name = "ShowcaseDepthRenderer";
                renderer.copyDepthMode = CopyDepthMode.AfterOpaques;
                AssetDatabase.CreateAsset(renderer,rendererPath);
            }
            string pipelinePath = Root+"/Rendering/ShowcaseDepthPipeline.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UnityEngine.Object.Instantiate(showcase.Pipeline); pipeline.name = "ShowcaseDepthPipeline";
                pipeline.supportsCameraDepthTexture = true;
                var serialized = new SerializedObject(pipeline);
                var list = serialized.FindProperty("m_RendererDataList"); list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                serialized.FindProperty("m_DefaultRendererIndex").intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline,pipelinePath);
            }
            Undo.RecordObject(showcase,"Devlog 전용 깊이 렌더러 연결"); showcase.Pipeline = pipeline;
            var cameraData = showcase.ShowcaseCamera.GetUniversalAdditionalCameraData();
            Undo.RecordObject(cameraData,"수면 깊이 텍스처 활성화"); cameraData.requiresDepthTexture = true;
        }

        private static Material WaterMaterial(Shader shader)
        {
            string path = Root+"/Materials/After_OceanDepth.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(shader) { name = "After_OceanDepth" };
            material.SetFloat("_BottomY",-LandHeight+BedHeight);
            AssetDatabase.CreateAsset(material,path); return material;
        }

        private static Material SideMaterial(string name, Color color)
        {
            string path = Root+"/Materials/"+name+".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Afterlife/Devlog/ShowcaseSurface")) { name = name };
            material.SetColor("_BaseColor",color); material.SetFloat("_VertexPalette",0);
            material.SetFloat("_Stylized",1);
            AssetDatabase.CreateAsset(material,path); return material;
        }

        private static Mesh UnitBlock()
        {
            string path = Root+"/Geometry/UnitTerrainBlock.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            var draft = new MeshDraft();
            draft.Quad(new Vector3(-.5f,1,-.5f),new Vector3(-.5f,1,.5f),new Vector3(.5f,1,.5f),new Vector3(.5f,1,-.5f),Vector4.zero);
            draft.Quad(new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f),Vector4.zero);
            draft.Quad(new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(-.5f,1,.5f),new Vector3(-.5f,1,-.5f),Vector4.zero);
            draft.Quad(new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,1,-.5f),new Vector3(.5f,1,.5f),Vector4.zero);
            draft.Quad(new Vector3(.5f,0,-.5f),new Vector3(-.5f,0,-.5f),new Vector3(-.5f,1,-.5f),new Vector3(.5f,1,-.5f),Vector4.zero);
            draft.Quad(new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,1,.5f),new Vector3(-.5f,1,.5f),Vector4.zero);
            mesh = draft.ToMesh("UnitTerrainBlock"); mesh.subMeshCount = 2;
            mesh.SetTriangles(draft.Indices.Take(6).ToArray(),0);
            mesh.SetTriangles(draft.Indices.Skip(6).ToArray(),1);
            AssetDatabase.CreateAsset(mesh,path); return mesh;
        }

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);
            go.transform.SetParent(parent,false); Undo.RegisterCreatedObjectUndo(go,"After 블록 지형 생성"); return go.transform;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/'); string parent = path.Substring(0,split); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,path.Substring(split+1));
        }

        private sealed class MeshDraft
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<Vector2> _uv = new List<Vector2>();
            private readonly List<Vector4> _shore = new List<Vector4>();
            public readonly List<int> Indices = new List<int>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector4 shore)
            {
                int start = _vertices.Count;
                _vertices.AddRange(new[]{a,b,c,d});
                Vector3 normal = Vector3.Cross(b-a,c-a).normalized;
                for (int i = 0; i < 4; i++) { _normals.Add(normal); _shore.Add(shore); }
                _uv.AddRange(new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)});
                Indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(_vertices); mesh.SetNormals(_normals); mesh.SetUVs(0,_uv); mesh.SetUVs(1,_shore);
                mesh.SetTriangles(Indices,0); mesh.RecalculateBounds(); return mesh;
            }
        }
    }
}
