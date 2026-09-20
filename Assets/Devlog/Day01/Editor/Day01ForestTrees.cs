using System;
using System.Linq;
using System.Collections.Generic;
using Afterlife.Devlog.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Afterlife.Devlog.Day01.Editor
{
    [InitializeOnLoad]
    public static class Day01ForestTrees
    {
        static Day01ForestTrees()
        {
            EditorApplication.delayCall+=Apply;
            EditorSceneManager.sceneSaved+=scene=>EditorApplication.delayCall+=Apply;
        }
        private static bool _running;
        private static void Apply()
        {
            if (_running) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall+=Apply; return; }
            var scene=SceneManager.GetSceneByPath(Day01SceneBuilder.ScenePath);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) return;
            if (opened) scene=EditorSceneManager.OpenScene(Day01SceneBuilder.ScenePath,OpenSceneMode.Additive);
            _running=true;
            try
            {
                var showcase=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day01Showcase>(true)).First();
                var stage=showcase.Stages[2]; var parent=stage.Find("ClickableResources");
                if (parent==null || parent.Cast<Transform>().Any(t=>t.name.StartsWith("ForestExtraTree_"))) return;
                var occupied=stage.GetComponentsInChildren<ShowcaseTarget>(true).Select(t=>t.transform.position).ToList();
                var obstacles=stage.Find("Environment").GetComponentsInChildren<Renderer>(true);
                string[] names={"Tree0_00","Tree2_02","Tree3_03"};
                int added=0;
                for (int z=0;z<=2 && added<3;z++) for (int x=-5;x<=-3 && added<3;x++)
                {
                    if (ShowcaseBiomeMap.At(x,z)!=ShowcaseBiome.Forest) continue;
                    var tile=stage.Find("Terrain_Flat/BiomeGrid_12x10").Cast<Transform>()
                        .FirstOrDefault(t=>Mathf.FloorToInt(t.localPosition.x)==x && Mathf.FloorToInt(t.localPosition.z)==z && t.name.EndsWith("_Forest"));
                    if (tile==null) continue;
                    float height=tile.GetComponent<MeshFilter>().sharedMesh.name.EndsWith("_Flat") ? 0 : (float)ShowcaseTileVariants.Height(x,z,.5,.5);
                    var world=stage.TransformPoint(new Vector3(x+.5f,height,z+.5f));
                    if (occupied.Any(p=>new Vector2(p.x-world.x,p.z-world.z).sqrMagnitude<1.0f)) continue;
                    if (obstacles.Any(o=>{ var b=o.bounds; b.Expand(.45f); return world.x>b.min.x && world.x<b.max.x && world.z>b.min.z && world.z<b.max.z; })) continue;
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Day01SceneBuilder.Root+"/Prefabs/Resources/"+names[added]+".prefab");
                    if (prefab==null) throw new InvalidOperationException("Forest tree prefab missing: "+names[added]);
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
                    Undo.RegisterCreatedObjectUndo(go,"숲 나무 추가");
                    go.name="ForestExtraTree_"+added; go.transform.position=world;
                    go.transform.localRotation=Quaternion.Euler(0,37+added*113,0); go.transform.localScale*=.8f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                    occupied.Add(world); added++;
                }
                if (added>0)
                {
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                    Debug.Log("Devlog forest trees added: "+added);
                }
                else Debug.LogWarning("Devlog forest: no clear planting positions found.");
            }
            finally { _running=false; if (opened) EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
