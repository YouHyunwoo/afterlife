using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Afterlife.Devlog.Day01.Editor
{
    // 최신 After를 한 번 캡처해 앞 단계를 만든다. 이후 Refresh로 사용자 배치를 덮어쓰지 않는다.
    [InitializeOnLoad]
    internal static class Day01StageProgressionSetup
    {
        private const string Root = Day01SceneBuilder.Root;
        private const string Output = Root + "/Prefabs/Stages/Progression";
        private const string Materials = Root + "/Materials/Progression";
        private static readonly string[] Names = { "Stage00_Before", "Stage01_Shape", "Stage02_Terrain", "Stage03_Life", "Stage04_Interaction" };
        private static bool _running;
        [Serializable] private sealed class Report
        {
            public string savedAtUtc, source, backup, snapshot;
            public string[] stages;
            public float[] positionsX;
            public int[] resourceCounts;
            public bool afterPreserved;
        }

        static Day01StageProgressionSetup()
        {
            EditorApplication.delayCall += Apply;
            EditorSceneManager.sceneSaved += _ => EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += state =>
            { if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Apply; };
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }

        private static void Apply()
        {
            if (_running || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += Apply; return; }
            if (!File.Exists(Day01SceneBuilder.ScenePath)) return;
            var scene = SceneManager.GetSceneByPath(Day01SceneBuilder.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) return;
            _running = true;
            var previous = SceneManager.GetActiveScene();
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(Day01SceneBuilder.ScenePath, OpenSceneMode.Additive);
                var showcase = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Day01Showcase>(true)).FirstOrDefault();
                if (showcase == null || showcase.HasPresentationStages || showcase.Stages == null || showcase.Stages.Length != 3) return;
                var after = showcase.Stages[2];
                if (after == null) throw new InvalidOperationException("Current After is missing.");
                var sourcePosition = after.position;
                int sourceCount = after.GetComponentsInChildren<ShowcaseTarget>(true).Length;
                Folder(Output); Folder(Materials); Folder(Root + "/Scenes/Archive");
                string backup = Root + "/Scenes/Archive/Devlog_Day01_BeforeProgression.unity";
                if (!File.Exists(backup) && !AssetDatabase.CopyAsset(Day01SceneBuilder.ScenePath, backup))
                    throw new IOException("Could not preserve original scene.");
                string snapshot = Output + "/AfterBaseline.prefab";
                var snapshotCopy = Object.Instantiate(after.gameObject);
                try
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(snapshotCopy))
                        PrefabUtility.UnpackPrefabInstance(snapshotCopy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    if (PrefabUtility.SaveAsPrefabAsset(snapshotCopy, snapshot) == null)
                        throw new IOException("Could not save After snapshot.");
                }
                finally { Object.DestroyImmediate(snapshotCopy); }

                var stages = new Transform[6];
                for (int phase = 0; phase < 5; phase++)
                {
                    // Instantiate remaps internal target/visual/surface references to this clone.
                    var copy = Object.Instantiate(after.gameObject, after.parent);
                    copy.name = Names[phase];
                    if (PrefabUtility.IsPartOfPrefabInstance(copy))
                        PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    var stage = stages[phase] = copy.transform;
                    stage.position = sourcePosition + Vector3.right * (phase * 20 - 100);
                    copy.SetActive(true);
                    foreach (var target in copy.GetComponentsInChildren<ShowcaseTarget>(true))
                    { target.EnableSnow = phase >= 3; target.Juicy = phase >= 4; }
                    if (phase < 3) HideDetails(stage);
                    CopyMaterials(stage, phase, stage.position - sourcePosition);
                    if (phase < 2) FlatGround(stage, phase);
                    if (phase == 0) PrimitiveModels(stage);
                    if (PrefabUtility.SaveAsPrefabAssetAndConnect(copy, Output + "/" + copy.name + ".prefab", InteractionMode.AutomatedAction) == null)
                        throw new IOException("Could not save stage: " + copy.name);
                }
                stages[5] = after;
                // 기존 nested prefab의 부모는 바꾸지 않고 비활성 참조로 보관한다.
                showcase.ArchivedStages = new[] { showcase.Stages[0], showcase.Stages[1] };
                foreach (var old in showcase.ArchivedStages)
                {
                    old.gameObject.SetActive(false);
                    old.name = "Archived_" + old.name;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
                }
                showcase.PresentationStages = stages;
                showcase.StartStage = 5;
                // Legacy migration scripts still find After at index 2.
                showcase.Stages = new[] { stages[0], stages[1], after };
                EditorUtility.SetDirty(showcase);
                PrefabUtility.RecordPrefabInstancePropertyModifications(showcase);
                if (PrefabUtility.SaveAsPrefabAsset(showcase.gameObject, Root + "/Prefabs/Composition/Day01Progression.prefab") == null)
                    throw new IOException("Could not save progression composition.");
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save progression scene.");
                var report = new Report {
                    savedAtUtc = DateTime.UtcNow.ToString("O"), source = after.name, backup = backup, snapshot = snapshot,
                    stages = stages.Select(t => t.name).ToArray(), positionsX = stages.Select(t => t.position.x).ToArray(),
                    resourceCounts = stages.Select(t => t.GetComponentsInChildren<ShowcaseTarget>(true).Length).ToArray(),
                    afterPreserved = after.position == sourcePosition && after.GetComponentsInChildren<ShowcaseTarget>(true).Length == sourceCount
                };
                File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../devlog/records/stage-progression-unity.json")), JsonUtility.ToJson(report, true));
                Debug.Log("Devlog: six presentation stages saved from current After. Keys 1-6; After remains at " + after.position);
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                _running = false;
            }
        }

        private static void HideDetails(Transform stage)
        {
            foreach (var child in stage.GetComponentsInChildren<Transform>(true))
                if (child.name == "GroundDetails" || child.name == "BiomeDetails" || child.name == "GroundContactDetails"
                    || child.name.StartsWith("TileDecoration_")) child.gameObject.SetActive(false);
        }

        private static void CopyMaterials(Transform stage, int phase, Vector3 offset)
        {
            var copies = new Dictionary<Material, Material>();
            foreach (var renderer in stage.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) continue;
                    if (!copies.TryGetValue(source, out var material))
                    {
                        string path = Materials + "/Step" + phase + "_" + copies.Count + ".mat";
                        material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (material == null) { material = new Material(source); AssetDatabase.CreateAsset(material, path); }
                        else material.CopyPropertiesFromMaterial(source);
                        material.name = "Step" + phase + "_" + source.name;
                        if (material.HasProperty("_TerrainMapBounds"))
                        {
                            var bounds = material.GetVector("_TerrainMapBounds");
                            bounds.x += offset.x; bounds.y += offset.z;
                            material.SetVector("_TerrainMapBounds", bounds);
                        }
                        if (phase < 3 && material.HasProperty("_Wind")) material.SetFloat("_Wind", 0);
                        foreach (string property in new[] { "_CrystalGlow", "_HouseWindows", "_HoverTerrain" })
                            if (material.HasProperty(property)) material.SetFloat(property, 0);
                        EditorUtility.SetDirty(material); copies.Add(source, material);
                    }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material FlatMaterial(string name, Color color)
        {
            string path = Materials + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            { material = new Material(Shader.Find("Afterlife/Devlog/ShowcaseSurface")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetFloat("_VertexPalette", 0);
            material.SetFloat("_TerrainBlend", 0); material.SetFloat("_Ground", 0); material.SetFloat("_Grid", 0);
            material.SetFloat("_Stylized", name.StartsWith("Before") ? 0 : 1);
            EditorUtility.SetDirty(material); return material;
        }

        private static Transform Group(Transform parent, string name)
        { var group = new GameObject(name).transform; group.SetParent(parent, false); return group; }

        private static void Primitive(Transform parent, string name, PrimitiveType shape, Vector3 center, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = center; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static void FlatGround(Transform stage, int phase)
        {
            var old = stage.Find("Terrain_Flat");
            if (old != null) { old.name = "ArchivedTerrain"; old.gameObject.SetActive(false); }
            var grid = Group(Group(stage, "Terrain_Flat"), "BiomeGrid_12x10");
            var palette = new Material[7];
            for (int i = 0; i < palette.Length; i++)
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/TerrainVariant_" + (ShowcaseBiome)i + ".mat");
                Color color = source != null ? source.GetColor("_BaseColor") : new Color(.15f, .42f, .57f);
                palette[i] = FlatMaterial(phase == 0 ? "BeforeGround" : "ShapeGround_" + (ShowcaseBiome)i,
                    phase == 0 ? new Color(.43f, .45f, .47f) : color);
            }
            for (int z = -5; z < 5; z++)
            for (int x = -6; x < 6; x++)
            {
                var biome = ShowcaseBiomeMap.At(x, z);
                Primitive(grid, "Tile_" + x + "_" + z + "_" + biome, PrimitiveType.Cube,
                    new Vector3(x + .5f, -.06f, z + .5f), new Vector3(.99f, .12f, .99f), palette[(int)biome]);
            }
            // XZ 배치는 그대로, 원본 지형의 작은 고도 보정만 평면으로 맞춘다.
            foreach (string name in new[] { "ClickableResources", "Environment" })
            {
                var group = stage.Find(name); if (group == null) continue;
                foreach (Transform item in group)
                { var p = item.localPosition; p.y = 0; item.localPosition = p; }
            }
        }

        private static Bounds LocalBounds(Transform root)
        {
            var bounds = new Bounds(); bool first = true;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var b = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var point = root.InverseTransformPoint(b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }

        private static void PrimitiveModels(Transform stage)
        {
            var gray = FlatMaterial("BeforeModels", new Color(.66f, .68f, .70f));
            foreach (var target in stage.GetComponentsInChildren<ShowcaseTarget>(true))
            {
                var bounds = LocalBounds(target.transform);
                target.Visual.gameObject.SetActive(false);
                target.Visual = Group(target.transform, "PrimitiveVisual");
                if (target.Wood)
                {
                    float height = Mathf.Max(.5f, bounds.size.y);
                    Primitive(target.Visual, "Trunk", PrimitiveType.Cylinder, new Vector3(bounds.center.x, height * .25f, bounds.center.z),
                        new Vector3(.22f, height * .25f, .22f), gray);
                    Primitive(target.Visual, "Crown", PrimitiveType.Sphere, new Vector3(bounds.center.x, height * .67f, bounds.center.z),
                        new Vector3(bounds.size.x, height * .66f, bounds.size.z), gray);
                }
                else Primitive(target.Visual, "Rock", PrimitiveType.Cube, bounds.center, bounds.size, gray);
            }
            var environment = stage.Find("Environment");
            if (environment == null) return;
            foreach (Transform pivot in environment)
            {
                var bounds = LocalBounds(pivot);
                foreach (Transform child in pivot) child.gameObject.SetActive(false);
                Primitive(pivot, "PrimitiveStructure", PrimitiveType.Cube, bounds.center, bounds.size, gray);
            }
        }
    }
}
