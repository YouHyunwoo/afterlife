using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Afterlife.Modules.Terrain;
using Afterlife.Modules.TerrainView;
using Afterlife.Modules.Water;
using Afterlife.Modules.Vegetation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace Afterlife.Devlog.Day02.Editor
{
    internal static class Day02FirstIslandArt
    {
        private const string ScenePath="Assets/Devlog/Youtube/Day02/Scenes/Devlog_Day02.unity";
        private const string After="Assets/Devlog/Youtube/Day01/";
        private const string Output="Assets/Devlog/Youtube/Day02/Geometry/ClickIslandAfter";
        private const string Marker="AfterIslandArt_v1";
        
        private static TerrainBiome Biome(int x,int z){float r=x*x/1.18f+z*z/.92f;return r<4.3f?TerrainBiome.Grass:r<10.7f?TerrainBiome.Sand:TerrainBiome.Water;}
        private static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Install;return;}
            if(!File.Exists(ScenePath)||File.ReadAllText(ScenePath).Contains(Marker))return;
            var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;
            if(!opened&&scene.isDirty){Debug.LogWarning("Day02 first island art: save the current scene before applying.");return;}
            if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try {
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day02Showcase>(true)).Single();
                var stage=view.Stages[0];
                var tree=stage.GetComponentsInChildren<Day02Resource>(true).First(r=>!r.Stone);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(After+"Prefabs/Resources/Tree0_00.prefab");
                var groundSource=AssetDatabase.LoadAssetAtPath<Material>(After+"Materials/AfterBlendedTerrain.mat");
                if(prefab==null||groundSource==null)throw new InvalidDataException("Day01 After sources missing.");
                Directory.CreateDirectory("../../../devlog/youtube/day-02/source/archive");
                File.Copy(ScenePath,"../../../devlog/youtube/day-02/source/archive/before-first-island-after.unity.txt",true);
                foreach(Transform child in stage.Cast<Transform>().ToArray())if(child!=tree.transform)UnityEngine.Object.DestroyImmediate(child.gameObject);
                foreach(Transform child in tree.Art.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                var source=UnityEngine.Object.Instantiate(prefab,tree.Art,false);source.name="Day01AfterTree";source.transform.localPosition=Vector3.zero;source.transform.localRotation=Quaternion.identity;
                foreach(var script in source.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
                foreach(var collider in source.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                var renderers=source.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                float scale=2.55f/bounds.size.y;source.transform.localScale*=scale;
                Vector3 center=tree.Art.InverseTransformPoint(bounds.center);source.transform.localPosition=-new Vector3(center.x,center.y-bounds.size.y*.5f,center.z)*scale;
                var terrainMaterial=new Material(groundSource){name="Day02AfterGround"};terrainMaterial.SetFloat("_TerrainBlend",0);terrainMaterial.SetFloat("_Grid",0);terrainMaterial.SetFloat("_Stylized",.25f);
                Color[] palette={new Color(.39f,.55f,.24f),new Color(.40f,.29f,.16f),new Color(.77f,.67f,.44f),new Color(.08f,.3f,.42f),Color.white,new Color(.3f,.47f,.21f),Color.gray};
                var terrain=new TerrainSurface(stage,7,Biome,terrainMaterial,palette,new CoastalTerrain(CoastShape.Rounded,-.12f,-.85f));
                // TerrainSurface는 입력 재질을 복제한다. 사용하지 않는 임시 원본은 즉시 해제한다.
                UnityEngine.Object.DestroyImmediate(terrainMaterial);
                var water=new WaterSurface(stage,7,(x,z)=>Biome(x,z)==TerrainBiome.Water,Shader.Find("Afterlife/Water/WaterSurface"),-.12f,-.85f);
                tree.transform.localPosition=new Vector3(-.35f,terrain.Height(-.35f,.25f),.25f);
                var box=tree.GetComponent<BoxCollider>();bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);box.center=tree.transform.InverseTransformPoint(bounds.center);box.size=bounds.size;
                var grass=new GrassRenderer(stage,new GrassRenderer.Map{Radius=3,SamplesPerAxis=7,Biome=Biome,Height=terrain.Height,Color=terrain.SurfaceColor},Shader.Find("Afterlife/Vegetation/Grass"));
                var surfaces=new List<LeafCardRenderer.Surface>();
                foreach(var filter in source.GetComponentsInChildren<MeshFilter>()){
                    var mesh=filter.sharedMesh;if(mesh==null||!mesh.isReadable)continue;
                    surfaces.Add(new LeafCardRenderer.Surface{Transform=filter.transform,Vertices=mesh.vertices,Normals=mesh.normals,Triangles=mesh.triangles,Colors=mesh.colors,Renderer=filter.GetComponent<Renderer>()});
                }
                var leaves=new LeafCardRenderer(surfaces,maxCards:1800);leaves.Tick(view.ShowcaseCamera);
                var marker=new GameObject(Marker);marker.transform.SetParent(stage,false);stage.gameObject.AddComponent<Day02IslandEnvironment>();
                // 에디터에서 생성된 형상을 직렬화 에셋으로 저장해 Play 전에도 동일하게 보인다.
                Directory.CreateDirectory(Output);AssetDatabase.Refresh();int meshIndex=0,materialIndex=0;
                foreach(var filter in stage.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh!=null&&string.IsNullOrEmpty(AssetDatabase.GetAssetPath(filter.sharedMesh)))
                    AssetDatabase.CreateAsset(filter.sharedMesh,AssetDatabase.GenerateUniqueAssetPath(Output+"/Mesh_"+(meshIndex++)+".asset"));
                foreach(var renderer in stage.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
                    if(material!=null&&string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath(Output+"/Material_"+(materialIndex++)+".mat"));
                view.ShowcaseCamera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                File.WriteAllText("../../../devlog/youtube/day-02/records/first-island-after.md","# 1번 섬 · Day01 After 표현 적용\n\n물 → 둥근 해안의 모래 해변 → 연속 풀 지형 → 나무 한 그루로 변경했다. Day01 After 나무 프리팹·지형 재질을 재사용하고, 공용 TerrainSurface/WaterSurface/GrassRenderer/LeafCardRenderer/WindFieldController를 조합했다. 2~4번 공간은 수정하지 않았다. 생성 메시·재질과 씬은 Unity에서 저장했다. 풀/나뭇잎은 GPU 카드 표현이며 클릭 채집을 유지한다. Play 검증은 미실행.\n\n생성 메시 "+meshIndex+"개, 재질 "+materialIndex+"개, 나뭇잎 카드 "+leaves.Count+"개.\n");
                Debug.Log("Day02 first island After art saved.");
            }catch(Exception error){Debug.LogException(error);}
            finally{if(opened&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
