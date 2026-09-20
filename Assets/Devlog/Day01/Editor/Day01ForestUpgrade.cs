using System.Linq;
using Afterlife.Devlog.Terrain;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    public static class Day01ForestUpgrade
    {
        public static void ApplyTo(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion>=12) return;
            var stage=showcase.Stages[2];
            var environment=stage.Find("Environment").GetComponentsInChildren<Renderer>();
            var resources=stage.GetComponentsInChildren<ShowcaseTarget>();
            var grid=stage.Find("Terrain_Flat/BiomeGrid_12x10");
            foreach (Transform tile in grid)
            {
                if (!tile.name.EndsWith("_Forest")) continue;
                int x=Mathf.FloorToInt(tile.localPosition.x),z=Mathf.FloorToInt(tile.localPosition.z);
                int seed=ShowcaseTileVariants.Seed(x,z);
                string asset=seed%4==0 ? "FallenLog" : "Fern";
                for (int attempt=0;attempt<4;attempt++)
                {
                    float u=.22f+(ShowcaseTileVariants.Seed(x+attempt*17,z+43)%1000)/999f*.56f;
                    float v=.22f+(ShowcaseTileVariants.Seed(x+71,z+attempt*13)%1000)/999f*.56f;
                    bool flat=tile.GetComponent<MeshFilter>().sharedMesh.name.EndsWith("_Flat");
                    Vector3 local=new Vector3(x+u,flat ? 0 : (float)ShowcaseTileVariants.Height(x,z,u,v),z+v);
                    Vector3 world=stage.TransformPoint(local);
                    if (environment.Any(r=>{ var b=r.bounds; b.Expand(.55f); return world.x>b.min.x && world.x<b.max.x && world.z>b.min.z && world.z<b.max.z; })) continue;
                    if (resources.Any(r=>{ var d=r.transform.position-world; d.y=0; return d.sqrMagnitude<.65f*.65f; })) continue;
                    if (tile.Cast<Transform>().Any(t=>{ var d=t.position-world; d.y=0; return d.sqrMagnitude<.36f*.36f; })) continue;
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/"+asset+".fbx");
                    if (prefab==null) break;
                    var pivot=new GameObject("TileDecoration_Forest_"+asset).transform;
                    Undo.RegisterCreatedObjectUndo(pivot.gameObject,"숲 전용 장식 배치");
                    pivot.SetParent(tile,false); pivot.position=world; pivot.rotation=Quaternion.Euler(0,seed%360,0);
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,pivot);
                    Undo.RegisterCreatedObjectUndo(instance,"숲 모델 연결");
                    var renderers=instance.GetComponentsInChildren<Renderer>();
                    Bounds bounds=renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float diameter=asset=="FallenLog" ? .65f : .40f;
                    pivot.localScale=Vector3.one*(diameter/Mathf.Max(.001f,Mathf.Max(bounds.size.x,bounds.size.z)));
                    float bottom=renderers.Min(r=>r.bounds.min.y);
                    pivot.position+=Vector3.up*(world.y-bottom);
                    var material=ForestMaterial(asset=="Fern");
                    foreach (var renderer in renderers)
                    {
                        renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        var filter=renderer.GetComponent<MeshFilter>();
                        if (filter==null || filter.sharedMesh==null) continue;
                        var surface=Undo.AddComponent<ShowcasePickSurface>(renderer.gameObject);
                        Day01ContentUpgrade.EnsureReadable(filter.sharedMesh); surface.VisibleRenderer=renderer;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
                    }
                    break;
                }
            }
            Undo.RecordObject(showcase,"숲 식생 버전 기록"); showcase.LayoutVersion=12;
            AssetDatabase.SaveAssets();
        }

        private static Material ForestMaterial(bool foliage)
        {
            string path=Day01SceneBuilder.Root+"/Materials/"+(foliage ? "ForestFern" : "ForestFallenLog")+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material!=null) return material;
            material=new Material(Shader.Find("Afterlife/Devlog/ShowcaseSurface"));
            material.SetColor("_BaseColor",foliage ? new Color(.48f,.66f,.44f) : new Color(.68f,.67f,.58f));
            material.SetFloat("_VertexPalette",1); material.SetFloat("_Stylized",.25f);
            material.SetFloat("_Wind",foliage ? .5f : 0);
            AssetDatabase.CreateAsset(material,path); return material;
        }
    }
}
