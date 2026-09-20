using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterlife.Devlog.Day01.Editor
{
    /// <summary>새 제단 FBX를 독립 프리팹으로 만들고 After의 기존 피벗에 배치한다.</summary>
    [InitializeOnLoad]
    internal static class Day01AltarModelSetup
    {
        internal const string ModelPath = Day01SceneBuilder.Root + "/Models/AbandonedAltarV2.fbx";
        private const string PrefabPath = Day01SceneBuilder.Root + "/Prefabs/Environment/AbandonedAltarV2.prefab";
        private const string MaterialPath = Day01SceneBuilder.Root + "/Materials/AbandonedAltarV2.mat";
        private static bool _running;

        static Day01AltarModelSetup()
        {
            EditorApplication.delayCall += Apply;
            EditorSceneManager.sceneSaved += _ => EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Apply;
            };
        }

        private static void Apply()
        {
            if (_running) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += Apply; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(ModelPath)) return;
            var scene = SceneManager.GetSceneByPath(Day01SceneBuilder.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            // 사용자 미저장 작업은 자동 저장하지 않고 다음 sceneSaved 이벤트까지 기다린다.
            if (!opened && scene.isDirty) return;
            _running = true;
            var previous = SceneManager.GetActiveScene();
            GameObject temporary = null;
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(Day01SceneBuilder.ScenePath, OpenSceneMode.Additive);
                var showcase = scene.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<Day01Showcase>(true)).FirstOrDefault();
                if (showcase == null || showcase.Stages.Length < 3 || showcase.Stages[2] == null) return;
                var pivot = showcase.Stages[2].Find("Environment/AbandonedAltar_Pivot");
                if (pivot == null || pivot.Find("AbandonedAltarV2") != null) return;

                var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
                if (importer == null) return;
                if (!importer.isReadable || importer.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    importer.isReadable = true;
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.importCameras = false;
                    importer.importLights = false;
                    importer.SaveAndReimport();
                }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (model == null) return;
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (material == null)
                {
                    var shader = Shader.Find("Afterlife/Devlog/ShowcaseSurface");
                    if (shader == null) throw new InvalidOperationException("ShowcaseSurface shader is missing.");
                    material = new Material(shader) { name = "AbandonedAltarV2" };
                    material.SetColor("_BaseColor", Color.white);
                    material.SetFloat("_VertexPalette", 1);
                    material.SetFloat("_Stylized", 1);
                    material.SetFloat("_Wind", 0);
                    AssetDatabase.CreateAsset(material, MaterialPath);
                }

                temporary = new GameObject("AbandonedAltarV2");
                SceneManager.MoveGameObjectToScene(temporary, scene);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, temporary.transform);
                visual.name = "AltarModel";
                // Blender FBX가 임포트된 축/단위 변환을 지우지 않는다.
                visual.transform.localPosition = model.transform.localPosition;
                visual.transform.localRotation = model.transform.localRotation;
                visual.transform.localScale = model.transform.localScale;
                var filters = visual.GetComponentsInChildren<MeshFilter>();
                if (filters.Length != 1) throw new InvalidOperationException("Altar FBX must have one combined mesh.");
                var filter = filters[0];
                var mesh = filter.sharedMesh;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || mesh == null || mesh.colors.Length != mesh.vertexCount)
                    throw new InvalidOperationException("Altar mesh/renderer/vertex palette is incomplete.");
                renderer.sharedMaterials = Enumerable.Repeat(material, mesh.subMeshCount).ToArray();
                // 실제 임포트 결과에서 누운 모델이나 잘못된 단위가 저장되는 것을 막는다.
                var size = renderer.bounds.size;
                if (size.y < 1.4f || size.y > 1.65f || size.x < 2.0f || size.x > 2.5f)
                    throw new InvalidOperationException("Unexpected altar import axes/scale: " + size);
                var surface = filter.gameObject.AddComponent<ShowcasePickSurface>();
                surface.VisibleRenderer = renderer;
                var prefab = PrefabUtility.SaveAsPrefabAsset(temporary, PrefabPath);
                UnityEngine.Object.DestroyImmediate(temporary);
                temporary = null;
                if (prefab == null) throw new InvalidOperationException("Could not save altar prefab.");

                // 원래 모델은 이 피벗의 비활성 자식으로 남겨 Inspector에서 비교할 수 있다.
                foreach (Transform child in pivot)
                {
                    Undo.RecordObject(child.gameObject, "이전 제단 보존");
                    child.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                }
                var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pivot);
                Undo.RegisterCreatedObjectUndo(placed, "새 제단 배치");
                placed.name = "AbandonedAltarV2";
                placed.transform.localPosition = new Vector3(0, .025f, 0);
                placed.transform.localRotation = Quaternion.identity;
                placed.transform.localScale = Vector3.one * .84f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(placed.transform);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Altar scene save failed.");
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.."));
                var receipt = new Receipt
                {
                    appliedAtUtc = DateTime.UtcNow.ToString("O"), scenePath = scene.path,
                    prefabPath = PrefabPath, rendererCount = placed.GetComponentsInChildren<Renderer>().Length,
                    triangleCount = mesh.triangles.Length / 3, importedSize = size,
                    placedScale = .84f, originalPreserved = true
                };
                File.WriteAllText(Path.Combine(projectRoot, "devlog/records/altar-v2-unity.json"), JsonUtility.ToJson(receipt, true));
                Debug.Log("Devlog: AbandonedAltarV2 prefab and After scene saved. Original altar preserved.", placed);
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (temporary != null) UnityEngine.Object.DestroyImmediate(temporary);
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                _running = false;
            }
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        [Serializable]
        private sealed class Receipt
        {
            public string appliedAtUtc, scenePath, prefabPath;
            public int rendererCount, triangleCount;
            public Vector3 importedSize;
            public float placedScale;
            public bool originalPreserved;
        }
    }

    internal sealed class Day01AltarModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (assetPath != Day01AltarModelSetup.ModelPath) return;
            var importer = (ModelImporter)assetImporter;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importNormals = ModelImporterNormals.Import;
        }
    }
}
