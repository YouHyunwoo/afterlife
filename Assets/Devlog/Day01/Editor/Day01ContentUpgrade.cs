using System;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Terrain;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    public static class Day01ContentUpgrade
    {
        private const string Root = Day01SceneBuilder.Root;
        private const string World = "Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/";
        private static readonly Color[] Palette =
        {
            new Color(.24f,.39f,.19f), new Color(.42f,.30f,.19f), new Color(.72f,.64f,.40f),
            new Color(.055f,.25f,.32f), new Color(.83f,.87f,.86f), new Color(.16f,.28f,.12f), new Color(.37f,.39f,.38f)
        };

        public static bool ApplyTo(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion >= 2) return false;
            if (showcase.LayoutVersion < 1) return false;
            for (int phase = 0; phase < showcase.Stages.Length; phase++)
            {
                var stage = showcase.Stages[phase];
                var terrain = stage.Find("Terrain_Flat");
                // 이전 바닥은 비활성 상태로 보존해 Undo 또는 비교에 쓸 수 있다.
                foreach (Transform child in terrain)
                {
                    Undo.RecordObject(child.gameObject, "기존 바닥 보존"); child.gameObject.SetActive(false);
                }
                var tiles = Group(terrain, "BiomeGrid_12x10");
                var materials = new Material[Palette.Length];
                for (int kind = 0; kind < materials.Length; kind++) materials[kind] = TileMaterial((ShowcaseBiome)kind, phase);
                for (int z = -5; z < 5; z++)
                for (int x = -6; x < 6; x++)
                {
                    var biome = ShowcaseBiomeMap.At(x,z);
                    var tile = GameObject.CreatePrimitive(phase == 0 ? PrimitiveType.Cube : PrimitiveType.Plane);
                    Undo.RegisterCreatedObjectUndo(tile, "바이옴 타일 생성");
                    tile.name = $"Tile_{x+6:00}_{z+5:00}_{biome}";
                    tile.transform.SetParent(tiles, false);
                    tile.transform.localPosition = new Vector3(x+.5f, phase == 0 ? -.02f : 0, z+.5f);
                    tile.transform.localScale = phase == 0 ? new Vector3(1,.04f,1) : new Vector3(.1f,1,.1f);
                    tile.GetComponent<Renderer>().sharedMaterial = materials[(int)biome];
                    UnityEngine.Object.DestroyImmediate(tile.GetComponent<Collider>());
                }
                MoveResourcesOffWater(stage);
                AddBiomeDetails(stage, phase);
                // 자원 표면과 가리는 환경 표면을 함께 등록한다. 타일은 클릭 대상이 아니다.
                foreach (string group in new[] { "ClickableResources", "Environment" })
                foreach (var filter in stage.Find(group).GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer = filter.GetComponent<Renderer>();
                    if (renderer == null || filter.sharedMesh == null) continue;
                    var surface = filter.GetComponent<ShowcasePickSurface>();
                    if (surface == null) surface = Undo.AddComponent<ShowcasePickSurface>(filter.gameObject);
                    Undo.RecordObject(surface, "표면 클릭 연결");
                    EnsureReadable(filter.sharedMesh);
                    surface.VisibleRenderer = renderer;
                    surface.Target = filter.GetComponentInParent<ShowcaseTarget>();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
                }
            }
            Undo.RecordObject(showcase, "바이옴/표면 클릭 버전 기록");
            showcase.LayoutVersion = 2;
            AssetDatabase.SaveAssets();
            Debug.Log("Devlog Day01: 360 flat biome tiles and mesh-surface picking installed.",showcase);
            return true;
        }

        internal static void EnsureReadable(Mesh mesh)
        {
            if (mesh == null || mesh.isReadable) return;
            string path = AssetDatabase.GetAssetPath(mesh);
            if (AssetImporter.GetAtPath(path) is ModelImporter importer)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            else throw new InvalidOperationException("Picking source is not readable: " + path);
        }

        public static void UpdatePath(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion >= 7) return;
            for (int phase=0;phase<showcase.Stages.Length;phase++)
            {
                var grid=showcase.Stages[phase].Find("Terrain_Flat/BiomeGrid_12x10");
                foreach (Transform tile in grid)
                {
                    if (!tile.name.StartsWith("Tile_") || !tile.gameObject.activeSelf) continue;
                    int x=Mathf.FloorToInt(tile.localPosition.x), z=Mathf.FloorToInt(tile.localPosition.z);
                    var biome=ShowcaseBiomeMap.At(x,z);
                    var renderer=tile.GetComponent<MeshRenderer>();
                    if (renderer == null || tile.name.EndsWith("_"+biome)) continue;
                    Undo.RecordObjects(new UnityEngine.Object[]{tile.gameObject,renderer},"현관 앞 길 갱신");
                    tile.name=$"Tile_{x+6:00}_{z+5:00}_{biome}";
                    var materials=renderer.sharedMaterials; materials[0]=TileMaterial(biome,phase);
                    if (phase==2 && materials.Length>1)
                        materials[1]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+(biome==ShowcaseBiome.Rocky ? "BlockRock" : "BlockSoil")+".mat");
                    renderer.sharedMaterials=materials;
                }
                var details=showcase.Stages[phase].Find("BiomeDetails");
                if (details != null)
                    foreach (Transform detail in details)
                    {
                        Vector3 p=showcase.Stages[phase].InverseTransformPoint(detail.position);
                        if (ShowcaseBiomeMap.At(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.z)) != ShowcaseBiome.Dirt) continue;
                        Undo.RecordObject(detail.gameObject,"길 위 장식 숨김"); detail.gameObject.SetActive(false);
                    }
            }
            Undo.RecordObject(showcase,"현관 길 버전 기록"); showcase.LayoutVersion=7;
            AssetDatabase.SaveAssets();
        }

        public static void AddPassiveClickSurfaces(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion>=10) return;
            var stage=showcase.Stages[2];
            foreach (var filter in stage.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.gameObject.activeInHierarchy || filter.sharedMesh==null) continue;
                var renderer=filter.GetComponent<Renderer>(); if (renderer==null || renderer.sharedMaterial==null) continue;
                var shader=renderer.sharedMaterial.shader;
                if (shader==null || (shader.name!="Afterlife/Devlog/ShowcaseSurface" && shader.name!="Afterlife/Devlog/ShowcaseWater")) continue;
                if (filter.GetComponent<ShowcasePickSurface>()!=null) continue;
                var surface=Undo.AddComponent<ShowcasePickSurface>(filter.gameObject);
                EnsureReadable(filter.sharedMesh); surface.VisibleRenderer=renderer;
                surface.Target=filter.GetComponentInParent<ShowcaseTarget>();
            }
            Undo.RecordObject(showcase,"비상호작용 클릭 피드백 버전"); showcase.LayoutVersion=10;
            AssetDatabase.SaveAssets();
        }

        private static Material TileMaterial(ShowcaseBiome biome, int phase)
        {
            string prefix = phase == 0 ? "Before" : phase == 1 ? "Process" : "After";
            string path = Root + "/Materials/" + prefix + "_Tile_" + biome + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Afterlife/Devlog/ShowcaseSurface")) { name = prefix + "_Tile_" + biome };
            float gray = .32f + (int)biome * .035f;
            material.SetColor("_BaseColor",phase == 0 ? new Color(gray,gray,gray) : Palette[(int)biome]);
            material.SetFloat("_VertexPalette",0);
            material.SetFloat("_Stylized",phase == 2 ? 1 : 0);
            material.SetFloat("_Ground",phase == 0 ? 0 : 1);
            material.SetFloat("_Grid",1);
            material.SetFloat("_Water",phase == 2 && biome == ShowcaseBiome.Water ? 1 : 0);
            AssetDatabase.CreateAsset(material,path);
            return material;
        }

        private static void MoveResourcesOffWater(Transform stage)
        {
            var resources = stage.Find("ClickableResources");
            foreach (Transform resource in resources)
            {
                Vector3 p = resource.localPosition;
                if (ShowcaseBiomeMap.At(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.z)) != ShowcaseBiome.Water) continue;
                Vector3 best = p; float nearest = float.PositiveInfinity;
                for (int z = -4; z < 4; z++)
                for (int x = -5; x < 5; x++)
                {
                    if (ShowcaseBiomeMap.At(x,z) == ShowcaseBiome.Water) continue;
                    Vector3 candidate = new Vector3(x+.5f,p.y,z+.5f);
                    if (resources.Cast<Transform>().Any(other => other != resource && Vector3.Distance(other.localPosition,candidate)<.9f)) continue;
                    float distance = (candidate-p).sqrMagnitude;
                    if (distance < nearest) { best = candidate; nearest = distance; }
                }
                Undo.RecordObject(resource,"수면 위 자원을 육지로 이동"); resource.localPosition = best;
            }
        }

        private static void AddBiomeDetails(Transform stage, int phase)
        {
            var old = stage.Find("GroundDetails");
            if (old != null) { Undo.RecordObject(old.gameObject,"기존 장식 보존"); old.gameObject.SetActive(false); }
            var group = Group(stage,"BiomeDetails");
            var random = new System.Random(1901);
            Material palette = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + (phase == 0 ? "BeforeObjects" : phase == 1 ? "ProcessModels" : "AfterFoliage") + ".mat");
            for (int z = -5; z < 5; z++)
            for (int x = -6; x < 6; x++)
            {
                var biome = ShowcaseBiomeMap.At(x,z);
                double chance = random.NextDouble();
                if (biome == ShowcaseBiome.Water || chance > (biome == ShowcaseBiome.Forest ? .75 : .32)) continue;
                Vector3 p = new Vector3(x+.5f,0,z+.5f);
                if (stage.Find("Environment").Cast<Transform>().Any(t => Vector3.Distance(t.localPosition,p)<1.25f)) continue;
                if (stage.Find("ClickableResources").Cast<Transform>().Any(t => Vector3.Distance(t.localPosition,p)<.65f)) continue;
                bool shore = ShowcaseBiomeMap.At(x+1,z)==ShowcaseBiome.Water || ShowcaseBiomeMap.At(x-1,z)==ShowcaseBiome.Water || ShowcaseBiomeMap.At(x,z+1)==ShowcaseBiome.Water || ShowcaseBiomeMap.At(x,z-1)==ShowcaseBiome.Water;
                string name = shore ? "Reeds" : biome == ShowcaseBiome.Forest ? "Fern" : biome == ShowcaseBiome.Grass ? "Flowers" : "Pebbles";
                var pivot = Group(group,name+"_Pivot_"+x+"_"+z);
                pivot.localPosition = p;
                pivot.localRotation = Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                pivot.localScale = Vector3.one*.8f;
                GameObject art;
                if (phase == 0)
                {
                    art = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Undo.RegisterCreatedObjectUndo(art,"장식 블록 생성");
                    art.transform.SetParent(pivot,false); art.transform.localPosition = Vector3.up*.1f; art.transform.localScale = new Vector3(.3f,.2f,.3f);
                    UnityEngine.Object.DestroyImmediate(art.GetComponent<Collider>());
                }
                else
                {
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(World+name+".fbx");
                    if (source == null) throw new InvalidOperationException("Missing biome detail: "+name);
                    art = (GameObject)PrefabUtility.InstantiatePrefab(source,pivot);
                    Undo.RegisterCreatedObjectUndo(art,"바이옴 장식 생성");
                }
                foreach (var renderer in art.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterials = Enumerable.Repeat(palette,renderer.sharedMaterials.Length).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }
        }

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);
            go.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(go,"Devlog 구성 생성");
            return go.transform;
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
