using System;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterlife.Devlog.Day01.Editor
{
    [InitializeOnLoad]
    internal static class Day01GroundContactSetup
    {
        private static bool _running;
        private const string World = "Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/";
        static Day01GroundContactSetup()
        {
            EditorApplication.delayCall += Apply;
            EditorSceneManager.sceneSaved += _ => EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += state =>
            { if (state==PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Apply; };
        }
        private static void Apply()
        {
            if (_running) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += Apply; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene=SceneManager.GetSceneByPath(Day01SceneBuilder.ScenePath);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) return;
            _running=true;
            var previous=SceneManager.GetActiveScene();
            try
            {
                if (opened) scene=EditorSceneManager.OpenScene(Day01SceneBuilder.ScenePath,OpenSceneMode.Additive);
                var showcase=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day01Showcase>(true)).FirstOrDefault();
                if (showcase==null || showcase.Stages.Length<3) return;
                var stage=showcase.Stages[2];
                if (stage.Find("GroundContactDetails")!=null) return;
                var pebbles=AssetDatabase.LoadAssetAtPath<GameObject>(World+"Pebbles.fbx");
                var fern=AssetDatabase.LoadAssetAtPath<GameObject>(World+"Fern.fbx");
                var material=AssetDatabase.LoadAssetAtPath<Material>(Day01SceneBuilder.Root+"/Materials/AfterModels.mat");
                if (pebbles==null || fern==null || material==null) return;
                var details=new GameObject("GroundContactDetails").transform;
                details.SetParent(stage,false);
                Undo.RegisterCreatedObjectUndo(details.gameObject,"바닥 접점 장식");
                int count=0, seated=0;
                foreach (var target in stage.GetComponentsInChildren<ShowcaseTarget>(true))
                {
                    var p=stage.InverseTransformPoint(target.transform.position);
                    var biome=ShowcaseBiomeMap.At(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.z));
                    if (biome!=ShowcaseBiome.Grass && biome!=ShowcaseBiome.Forest && biome!=ShowcaseBiome.Rocky) continue;
                    float angle=(ShowcaseTileVariants.Seed(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.z))%360)*Mathf.Deg2Rad;
                    for (int i=0;i<2;i++)
                    {
                        float a=angle+i*.85f;
                        float radius=target.Wood ? .26f+i*.10f : .39f+i*.08f;
                        Vector3 pos=p+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                        bool green=i==1 && biome!=ShowcaseBiome.Rocky;
                        if (Place(stage,details,green ? fern : pebbles,material,pos,green ? .22f : .30f,a*Mathf.Rad2Deg,count)) count++;
                    }
                }
                foreach (string name in new[]{"House_Pivot","AbandonedAltar_Pivot"})
                {
                    var pivot=stage.Find("Environment/"+name);
                    if (pivot==null) continue;
                    var renderers=pivot.GetComponentsInChildren<Renderer>();
                    if (renderers.Length==0) continue;
                    var bounds=renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    var center=stage.InverseTransformPoint(pivot.position);
                    // 기단 최저점을 실제 타일 표면보다 1.5cm 아래에 맞춘다.
                    float desired=stage.TransformPoint(new Vector3(center.x,Ground(stage,center),center.z)).y-.015f;
                    float shift=Mathf.Clamp(desired-bounds.min.y,-.08f,.08f);
                    Undo.RecordObject(pivot,"기단 접지");
                    pivot.position+=Vector3.up*shift;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pivot);
                    seated++;
                    foreach (var corner in new[]{new Vector3(bounds.min.x,0,bounds.max.z),new Vector3(bounds.max.x,0,bounds.max.z)})
                    {
                        Vector3 p=stage.InverseTransformPoint(corner);
                        if (Place(stage,details,pebbles,material,p,.27f,37+count*61,count)) count++;
                    }
                }
                PrefabUtility.SaveAsPrefabAssetAndConnect(details.gameObject,
                    Day01SceneBuilder.Root+"/Prefabs/Environment/GroundContactDetails.prefab",InteractionMode.AutomatedAction);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Ground contact scene save failed.");
                string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../.."));
                File.WriteAllText(Path.Combine(root,"devlog/youtube/records/ground-contact-unity.json"),
                    "{\"savedAtUtc\":\""+DateTime.UtcNow.ToString("O")+"\",\"details\":"+count+",\"seatedStructures\":"+seated+"}");
                Debug.Log("Devlog ground contact: "+count+" existing props, "+seated+" structure bases saved.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene,true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                _running=false;
            }
        }
        private static float Ground(Transform stage,Vector3 p)
        {
            int x=Mathf.FloorToInt(p.x),z=Mathf.FloorToInt(p.z);
            var grid=stage.Find("Terrain_Flat/BiomeGrid_12x10");
            if (grid!=null)
                foreach (Transform tile in grid)
                {
                    if (Mathf.FloorToInt(tile.localPosition.x)!=x || Mathf.FloorToInt(tile.localPosition.z)!=z) continue;
                    var surface=tile.GetComponent<ShowcasePickSurface>();
                    var start=stage.TransformPoint(new Vector3(p.x,20,p.z));
                    if (surface!=null && surface.Intersect(new Ray(start,Vector3.down),out float distance,out _))
                        return stage.InverseTransformPoint(start-Vector3.up*distance).y;
                    var filter=tile.GetComponent<MeshFilter>();
                    if (filter!=null && filter.sharedMesh!=null && filter.sharedMesh.name.EndsWith("_Flat")) return 0;
                }
            return (float)ShowcaseTileVariants.Height(x,z,p.x-x,p.z-z);
        }
        private static bool Place(Transform stage,Transform parent,GameObject asset,Material material,Vector3 p,float scale,float yaw,int index)
        {
            if (p.x<=-5.85f || p.x>=5.85f || p.z<=-4.85f || p.z>=4.85f) return false;
            var biome=ShowcaseBiomeMap.At(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.z));
            if (biome!=ShowcaseBiome.Grass && biome!=ShowcaseBiome.Forest && biome!=ShowcaseBiome.Rocky) return false;
            if (asset.name=="Fern" && biome==ShowcaseBiome.Rocky) return false;
            p.y=Ground(stage,p)-.007f;
            var holder=new GameObject("GroundContact_"+index+"_"+asset.name).transform;
            holder.SetParent(parent,false); holder.localPosition=p;
            holder.localRotation=Quaternion.Euler(0,yaw,0); holder.localScale=Vector3.one*scale;
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(asset,holder);
            visual.transform.localPosition=asset.transform.localPosition;
            visual.transform.localRotation=asset.transform.localRotation;
            visual.transform.localScale=asset.transform.localScale;
            foreach (var filter in visual.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<Renderer>();
                if (renderer==null || filter.sharedMesh==null) continue;
                var foliage=asset.name=="Fern" ? AssetDatabase.LoadAssetAtPath<Material>(Day01SceneBuilder.Root+"/Materials/AfterFoliage.mat") : null;
                renderer.sharedMaterials=Enumerable.Repeat(foliage!=null ? foliage : material,filter.sharedMesh.subMeshCount).ToArray();
                var surface=filter.gameObject.AddComponent<ShowcasePickSurface>();
                Day01ContentUpgrade.EnsureReadable(filter.sharedMesh); surface.VisibleRenderer=renderer;
            }
            return true;
        }
    }
}
