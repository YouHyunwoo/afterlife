using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Afterlife.Devlog.Terrain;

namespace Afterlife.Devlog.Day01.Editor
{
    // 초기 버전 씬만 보정한다. 새로 배치하거나 사용자의 위치/크기를 초기화하지 않는다.
    [InitializeOnLoad]
    public static class Day01AxisMigration
    {
        private const string World = "Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/";
        private static bool _applying;

        static Day01AxisMigration()
        {
            EditorApplication.delayCall += Apply;
            EditorSceneManager.sceneOpened += (_, __) =>
            {
                if (!_applying) EditorApplication.delayCall += Apply;
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Apply;
            };
        }

        public static void ApplySnowLayout(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion >= 4) return;
            // 세 단계의 비교 구도를 유지하면서 기존 돌 하나를 눈 구역에 배치한다.
            foreach (var stage in showcase.Stages)
            {
                var rock = stage.GetComponentsInChildren<ShowcaseTarget>(true).LastOrDefault(target => !target.Wood);
                if (rock == null) continue;
                Undo.RecordObject(rock.transform, "눈 바이옴 돌 배치");
                rock.transform.position = stage.TransformPoint(new Vector3(-4.6f,0,4.2f));
            }
            Undo.RecordObject(showcase, "눈 바이옴 버전 기록");
            showcase.LayoutVersion = 4;
        }

        public static void ApplyBeachLayout(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion >= 5) return;
            foreach (var stage in showcase.Stages)
            {
                var targets = stage.GetComponentsInChildren<ShowcaseTarget>(true);
                foreach (var tree in targets.Where(target => target.Wood))
                {
                    Vector3 position = stage.InverseTransformPoint(tree.transform.position);
                    var biome = ShowcaseBiomeMap.At(Mathf.FloorToInt(position.x),Mathf.FloorToInt(position.z));
                    if (biome != ShowcaseBiome.Sand && biome != ShowcaseBiome.Water) continue;
                    Vector3 best = position;
                    float nearest = float.PositiveInfinity;
                    for (int z=-4;z<5;z++)
                    for (int x=-5;x<6;x++)
                    {
                        var candidateBiome = ShowcaseBiomeMap.At(x,z);
                        if (candidateBiome != ShowcaseBiome.Grass && candidateBiome != ShowcaseBiome.Forest && candidateBiome != ShowcaseBiome.Snow) continue;
                        Vector3 candidate = new Vector3(x+.5f,position.y,z+.5f);
                        if (targets.Any(other => other != tree && Vector3.Distance(stage.InverseTransformPoint(other.transform.position),candidate)<1.25f)) continue;
                        float distance = (candidate-position).sqrMagnitude;
                        if (distance < nearest) { best=candidate; nearest=distance; }
                    }
                    if (float.IsPositiveInfinity(nearest)) throw new InvalidOperationException("No inland tree placement available.");
                    Undo.RecordObject(tree.transform,"해변 나무를 내륙으로 이동");
                    tree.transform.position = stage.TransformPoint(best);
                }
            }
            Undo.RecordObject(showcase,"해변 배치 버전 기록");
            showcase.LayoutVersion = 5;
        }

        private static void Apply()
        {
            if (_applying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += Apply; return; }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Day01SceneBuilder.ScenePath) == null) return;
            Scene scene = SceneManager.GetSceneByPath(Day01SceneBuilder.ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            Scene previous = SceneManager.GetActiveScene();
            _applying = true;
            try
            {
                if (openedHere) scene = EditorSceneManager.OpenScene(Day01SceneBuilder.ScenePath, OpenSceneMode.Additive);
                bool wasDirty = scene.isDirty;
                var showcase = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Day01Showcase>(true)).FirstOrDefault();
                if (showcase == null || showcase.LayoutVersion >= 12) return;
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Devlog 모델 축 보정");
                try
                {
                    int count = 0;
                    var models = showcase.LayoutVersion >= 1 ? Array.Empty<Transform>() : showcase.GetComponentsInChildren<Transform>(true)
                        .Where(t => PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject) == t.gameObject
                            && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).StartsWith(World, StringComparison.Ordinal))
                        .ToArray();
                    foreach (var model in models)
                    {
                        var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                        if (source == null) throw new InvalidOperationException("Missing source model: " + model.name);
                        Quaternion importRotation = source.transform.localRotation;
                        // 기존 값 = importRotation * authoredYaw. authoredYaw를 피벗으로 옮긴다.
                        Quaternion authoredYaw = Quaternion.Inverse(importRotation) * model.localRotation;
                        var pivot = new GameObject(model.name + "_Pivot");
                        SceneManager.MoveGameObjectToScene(pivot, scene);
                        Undo.RegisterCreatedObjectUndo(pivot, "모델 피벗 생성");
                        pivot.transform.SetParent(model.parent, false);
                        pivot.transform.SetSiblingIndex(model.GetSiblingIndex());
                        pivot.transform.localPosition = model.localPosition;
                        pivot.transform.localRotation = authoredYaw;
                        Vector3 sourceScale = source.transform.localScale;
                        Vector3 scale = model.localScale;
                        pivot.transform.localScale = new Vector3(scale.x/sourceScale.x, scale.y/sourceScale.y, scale.z/sourceScale.z);
                        pivot.SetActive(model.gameObject.activeSelf);
                        Undo.SetTransformParent(model, pivot.transform, "모델 축 보정");
                        Undo.RecordObject(model, "FBX 임포트 회전 복구");
                        model.localPosition = Vector3.zero;
                        model.localRotation = importRotation;
                        model.localScale = sourceScale;
                        Undo.RecordObject(model.gameObject, "도구 활성 상태 이동");
                        model.gameObject.SetActive(true);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(model);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(model.gameObject);
                        var target = pivot.GetComponentInParent<ShowcaseTarget>();
                        if (target != null && target.Visual == model)
                        {
                            Undo.RecordObject(target, "타격 피벗 연결");
                            target.Visual = pivot.transform;
                            RefitCollider(target);
                        }
                        Undo.RecordObject(showcase, "도구 피벗 연결");
                        if (showcase.Axe == model.gameObject) showcase.Axe = pivot;
                        if (showcase.Pick == model.gameObject) showcase.Pick = pivot;
                        count++;
                    }
                    Undo.RecordObject(showcase, "씬 보정 버전 기록");
                    showcase.LayoutVersion = Math.Max(1,showcase.LayoutVersion);
                    Day01ContentUpgrade.ApplyTo(showcase);
                    Day01WaterUpgrade.ApplyTo(showcase);
                    ApplySnowLayout(showcase);
                    ApplyBeachLayout(showcase);
                    Day01WaterUpgrade.ApplySeparatedLayers(showcase);
                    Day01ContentUpgrade.UpdatePath(showcase);
                    Day01TerrainVariants.ApplyTo(showcase);
                    Day01ContentUpgrade.AddPassiveClickSurfaces(showcase);
                    Day01BiomeBlendUpgrade.ApplyTo(showcase);
                    Day01ForestUpgrade.ApplyTo(showcase);
                    EditorSceneManager.MarkSceneDirty(scene);
                    // 사용자의 미저장 편집이 있으면 현재 씬에만 적용하고 저장은 맡긴다.
                    if (!wasDirty && !EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Devlog axis correction save failed.");
                    Undo.CollapseUndoOperations(group);
                    Debug.Log($"Devlog Day01 axis correction: {count} model pivots repaired, resource colliders refitted. "
                        + (wasDirty ? "Scene has unsaved user edits; save the scene to persist." : "Scene saved."), showcase);
                }
                catch { Undo.RevertAllDownToGroup(group); throw; }
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (openedHere && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                _applying = false;
            }
        }

        private static void RefitCollider(ShowcaseTarget target)
        {
            var box = target.GetComponent<BoxCollider>();
            var renderers = target.Visual.GetComponentsInChildren<Renderer>(true);
            if (box == null || renderers.Length == 0) return;
            Bounds bounds = new Bounds();
            bool first = true;
            foreach (var renderer in renderers)
            {
                Bounds world = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = world.center + Vector3.Scale(world.extents, new Vector3((i&1)==0 ? -1 : 1, (i&2)==0 ? -1 : 1, (i&4)==0 ? -1 : 1));
                    Vector3 local = target.transform.InverseTransformPoint(corner);
                    if (first) { bounds = new Bounds(local, Vector3.zero); first = false; }
                    else bounds.Encapsulate(local);
                }
            }
            Undo.RecordObject(box, "수직 모델에 맞게 클릭 영역 보정");
            box.center = bounds.center;
            box.size = bounds.size;
        }
    }
}
