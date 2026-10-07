using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Afterlife.Devlog.Day02;
using Afterlife.Modules.SceneFeatures;
using Afterlife.Modules.SceneFeatures.Editor;
using Afterlife.Modules.Terrain;
using Afterlife.Modules.TerrainView;
using Afterlife.Modules.Interaction;

namespace Afterlife.Devlog.Editor
{
    // Explicit authoring command only. Runtime consumes saved data and shared feature services.
    internal static class Day02IslandArtMigration
    {
        private const string Root="Assets/Devlog/Youtube/Day02";
        private const string Source="Assets/Devlog/Youtube/Day01/Prefabs";
        private const string Marker="AfterIslandComposition_v2";
        private static T Load<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
        // Explicit install; no Tools menu or automatic scene rewrite.
        public static string InstallHarvestFeedback()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            const string scenePath=Root+"/Scenes/Devlog_Day02.unity";
            var scene=SceneManager.GetSceneByPath(scenePath);bool opened=!scene.isLoaded;
            if(!opened&&scene.isDirty)throw new InvalidOperationException("Save the existing Day02 scene changes first.");
            var source=Load<GameObject>(Source+"/Composition/Day01Showcase.prefab").GetComponent<Afterlife.Devlog.Day01.Day01Showcase>();
            var wood=Load<Sprite>("Assets/Prototype/Resources/UI/SceneSprites/Icon0.asset");
            var stone=Load<Sprite>("Assets/Prototype/Resources/UI/SceneSprites/Icon1.asset");
            if(source==null||wood==null||stone==null)throw new InvalidOperationException("Day01 tools or prototype collection sprites are missing.");
            const string islandPath=Root+"/Prefabs/SmallHarvestIsland.prefab";
            var island=PrefabUtility.LoadPrefabContents(islandPath);
            try {
                MatchHarvestTools(island.transform,source.Axe,source.Pick);
                PrefabUtility.SaveAsPrefabAsset(island,islandPath);
            } finally {PrefabUtility.UnloadPrefabContents(island);}
            if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try {
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day02Showcase>(true)).Single();
                foreach(var stage in view.Stages)MatchHarvestTools(stage,source.Axe,source.Pick);
                view.WoodCollectionSprite=wood;view.StoneCollectionSprite=stone;
                EditorUtility.SetDirty(view);PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Day02 save failed.");
                AssetDatabase.SaveAssets();
                return "Saved Day02 scene and SmallHarvestIsland: Day01 tool transforms and blade contacts; wood/stone collection sprites assigned. Play not run.";
            } finally {if(opened&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
        }
        private static void MatchHarvestTools(Transform stage,GameObject axe,GameObject pick)
        {
            var resources=stage.GetComponentsInChildren<Day02Resource>(true);
            foreach(var resource in resources) {
                var tool=resource.Tool;if(tool==null)throw new InvalidOperationException("Tool missing: "+resource.name);
                var original=resource.Stone?pick:axe;
                var sourceArt=original.transform.Cast<Transform>().First(t=>t.GetComponentsInChildren<Renderer>(true).Length>0);
                var art=tool.transform.Cast<Transform>().First(t=>t.GetComponentsInChildren<Renderer>(true).Length>0);
                art.localPosition=sourceArt.localPosition;art.localRotation=sourceArt.localRotation;art.localScale=sourceArt.localScale;
                tool.transform.localPosition=Vector3.zero;tool.transform.localRotation=Quaternion.identity;tool.transform.localScale=Vector3.one;
                tool.SetActive(false);
                EditorUtility.SetDirty(art);EditorUtility.SetDirty(tool.transform);EditorUtility.SetDirty(tool);
                PrefabUtility.RecordPrefabInstancePropertyModifications(art);PrefabUtility.RecordPrefabInstancePropertyModifications(tool.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(tool);
            }
            var host=stage.GetComponent<SceneFeatureHost>();
            if(host==null)throw new InvalidOperationException("SceneFeatureHost missing: "+stage.name);
            host.Bindings.Axe=resources.FirstOrDefault(r=>!r.Stone)?.Tool;host.Bindings.Pick=resources.FirstOrDefault(r=>r.Stone)?.Tool;
            host.Bindings.AxeContact=BladeContact(host.Bindings.Axe);host.Bindings.PickContact=BladeContact(host.Bindings.Pick);
            EditorUtility.SetDirty(host);PrefabUtility.RecordPrefabInstancePropertyModifications(host);
        }
        private static Transform BladeContact(GameObject tool)
        {
            if(tool==null)return null;
            // Ignore the old zero-position anchor under ToolPivot; use the FBX blade marker.
            var art=tool.transform.Cast<Transform>().First(t=>t.GetComponentsInChildren<Renderer>(true).Length>0);
            return art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="ImpactPoint"||t.name.StartsWith("ImpactPoint."));
        }
        public static string CaptureViews()
        {
            const string scenePath=Root+"/Scenes/Devlog_Day02.unity";
            var scene=SceneManager.GetSceneByPath(scenePath);bool opened=!scene.isLoaded;
            if(!opened&&scene.isDirty)throw new InvalidOperationException("Day02 씬의 기존 변경을 먼저 저장하세요.");
            if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            var previousScene=SceneManager.GetActiveScene();bool dirty=scene.isDirty;
            var previousPipeline=GraphicsSettings.defaultRenderPipeline;var previousQuality=QualitySettings.renderPipeline;
            var layers=new Dictionary<GameObject,int>();
            try {
                SceneManager.SetActiveScene(scene);
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day02Showcase>(true)).Single();
                var camera=view.ShowcaseCamera;var position=camera.transform.position;var rotation=camera.transform.rotation;
                float size=camera.orthographicSize;int culling=camera.cullingMask;var target=camera.targetTexture;
                var active=view.Stages.Select(s=>s.gameObject.activeSelf).ToArray();
                GraphicsSettings.defaultRenderPipeline=view.Pipeline;QualitySettings.renderPipeline=view.Pipeline;
                try {
                    foreach(var stage in view.Stages)foreach(var item in stage.GetComponentsInChildren<Transform>(true)) {layers[item.gameObject]=item.gameObject.layer;item.gameObject.layer=30;}
                    camera.cullingMask=1<<30;
                    Directory.CreateDirectory("Temp/Day02VisualReview");
                    var offset=new Vector3(7,10,-12);
                    for(int index=0;index<view.Stages.Length;index++) {
                        for(int i=0;i<view.Stages.Length;i++)view.Stages[i].gameObject.SetActive(i==index);
                        camera.transform.SetPositionAndRotation(view.Stages[index].position+Vector3.up*.6f+offset,Quaternion.LookRotation(-offset));
                        camera.orthographicSize=view.SceneVersion>=4?3.8f:index==3?7:3.2f;
                        var texture=RenderTexture.GetTemporary(1280,720,24);
                        var previousActive=RenderTexture.active;
                        try {
                            camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
                            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                            try { image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Temp/Day02VisualReview/stage-"+(index+1)+".png",image.EncodeToPNG()); }
                            finally {UnityEngine.Object.DestroyImmediate(image);}
                        } finally {camera.targetTexture=target;RenderTexture.active=previousActive;RenderTexture.ReleaseTemporary(texture);}
                    }
                } finally {
                    for(int i=0;i<view.Stages.Length;i++)view.Stages[i].gameObject.SetActive(active[i]);
                    foreach(var layer in layers)if(layer.Key!=null)layer.Key.layer=layer.Value;
                    camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;camera.cullingMask=culling;camera.targetTexture=target;
                }
                return "SUCCESS: four edit-mode camera images in Temp/Day02VisualReview. Play/Test Runner not run.";
            } finally {
                GraphicsSettings.defaultRenderPipeline=previousPipeline;QualitySettings.renderPipeline=previousQuality;
                if(previousScene.IsValid()&&previousScene.isLoaded)SceneManager.SetActiveScene(previousScene);
                if(opened&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
                else if(!dirty && scene.IsValid()&&scene.isLoaded)EditorSceneManager.SaveScene(scene);
            }
        }
        public static string ReportScale()
        {
            var b=new System.Text.StringBuilder();
            var after=Load<GameObject>(Source+"/Stages/After_PixelForest.prefab");
            if(after==null)throw new FileNotFoundException("Day01 After stage");
            b.AppendLine("Day01 After stage:");
            foreach(var target in after.GetComponentsInChildren<Afterlife.Devlog.Day01.ShowcaseTarget>(true)) {
                var bounds=BoundsOf(target.gameObject);
                b.AppendLine(target.name+" scale="+target.transform.localScale+" bounds="+bounds.size);
            }
            foreach(var model in new[]{"Tree0_00","Tree1_01","Tree2_02","Tree3_03","Rock0_09","Rock1_10","Rock2_11","Rock3_12"}) {
                var prefab=Load<GameObject>(Source+"/Resources/"+model+".prefab");
                b.AppendLine("Prefab "+model+" scale="+prefab.transform.localScale+" bounds="+BoundsOf(prefab).size);
            }
            var day01Scene=SceneManager.GetSceneByPath("Assets/Devlog/Youtube/Day01/Scenes/Devlog_Day01.unity");bool openedDay01=!day01Scene.isLoaded;
            if(openedDay01)day01Scene=EditorSceneManager.OpenScene("Assets/Devlog/Youtube/Day01/Scenes/Devlog_Day01.unity",OpenSceneMode.Additive);
            try {
                var day01=day01Scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Afterlife.Devlog.Day01.Day01Showcase>(true)).Single();
                foreach(var tool in new[]{day01.Axe,day01.Pick})
                    b.AppendLine("Day01 tool "+tool.name+" scale="+tool.transform.localScale+" bounds="+BoundsOf(tool).size);
            } finally {if(openedDay01)EditorSceneManager.CloseScene(day01Scene,true);}
            var scene=SceneManager.GetSceneByPath(Root+"/Scenes/Devlog_Day02.unity");bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(Root+"/Scenes/Devlog_Day02.unity",OpenSceneMode.Additive);
            try {
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day02Showcase>(true)).Single();
                for(int i=0;i<view.Stages.Length;i++) {
                    var stage=view.Stages[i];b.AppendLine("Day02 stage "+i+" scale="+stage.localScale);
                    foreach(var resource in stage.GetComponentsInChildren<Day02Resource>(true).Take(5))
                        b.AppendLine(resource.name+" position="+resource.transform.localPosition+" artBounds="+BoundsOf(resource.Art.gameObject).size+
                            " toolBounds="+(resource.Tool!=null?BoundsOf(resource.Tool).size:Vector3.zero));
                }
            } finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
            return b.ToString();
        }
        public static string ApplyDay01Scale()
        {
            string path=Root+"/Scenes/Devlog_Day02.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(!opened&&scene.isDirty)throw new InvalidOperationException("Day02 씬의 기존 변경을 먼저 저장하세요.");
            Directory.CreateDirectory("../../../devlog/youtube/day-02/source/archive");
            File.Copy(path,"../../../devlog/youtube/day-02/source/archive/before-day01-scale.unity.txt",true);
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var report=new System.Text.StringBuilder();
            try {
                var day01Scene=SceneManager.GetSceneByPath("Assets/Devlog/Youtube/Day01/Scenes/Devlog_Day01.unity");bool openedDay01=!day01Scene.isLoaded;
                if(openedDay01)day01Scene=EditorSceneManager.OpenScene("Assets/Devlog/Youtube/Day01/Scenes/Devlog_Day01.unity",OpenSceneMode.Additive);
                float axeHeight,pickHeight;
                try {
                    var day01=day01Scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Afterlife.Devlog.Day01.Day01Showcase>(true)).Single();
                    axeHeight=BoundsOf(day01.Axe).size.y;pickHeight=BoundsOf(day01.Pick).size.y;
                } finally {if(openedDay01)EditorSceneManager.CloseScene(day01Scene,true);}
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day02Showcase>(true)).Single();
                var active=view.Stages.Select(t=>t.gameObject.activeSelf).ToArray();
                foreach(var stage in view.Stages)stage.gameObject.SetActive(false);
                try {
                    for(int index=0;index<view.Stages.Length;index++) {
                        var stage=view.Stages[index];var host=stage.GetComponent<SceneFeatureHost>();
                        if(host==null)throw new InvalidOperationException("Feature host missing: "+stage.name);
                        host.Clear();stage.localScale=Vector3.one;
                        if(index==0) {
                            var art=stage.GetComponentsInChildren<Day02Resource>(true).Single().Art;
                            var tree=art.Find("Day01AfterTree");if(tree==null)throw new InvalidOperationException("First island tree missing");
                            var prefab=Load<GameObject>(Source+"/Resources/Tree0_00.prefab");
                            SetPrefabScale(tree.gameObject,prefab,art,false);
                            SyncCollider(art.GetComponentInParent<Day02Resource>(),tree.gameObject);
                        } else {
                            var resources=stage.GetComponentsInChildren<Day02Resource>(true);
                            for(int n=0;n<resources.Length;n++) {
                                var resource=resources[n];var art=resource.Art;
                                var child=art.Cast<Transform>().FirstOrDefault(t=>t.GetComponentsInChildren<Renderer>(true).Length>0);
                                if(child==null)throw new InvalidOperationException("Resource art missing: "+resource.name);
                                string model=resource.Stone?new[]{"Rock0_09","Rock1_10","Rock2_11","Rock3_12"}[n%4]:new[]{"Tree0_00","Tree1_01","Tree2_02","Tree3_03"}[n%4];
                                var prefab=Load<GameObject>(Source+"/Resources/"+model+".prefab");
                                SetPrefabScale(child.gameObject,prefab,art,true);
                                SyncCollider(resource,child.gameObject);
                            }
                            if(index==3) {
                                var house=stage.Find("AfterHouse");if(house==null)throw new InvalidOperationException("After house missing");
                                var prefab=Load<GameObject>(Source+"/Environment/House_Pivot.prefab");
                                SetPrefabScale(house.gameObject,prefab,stage,true);
                                var p=house.localPosition;p.y+=(float)Afterlife.Modules.Terrain.TerrainHeightField.Height(0,.5f);p.z+=.5f;house.localPosition=p;
                            }
                        }
                        foreach(var resource in stage.GetComponentsInChildren<Day02Resource>(true))
                            MatchDay01Tool(resource,resource.Stone?pickHeight:axeHeight);
                        host.Apply();host.Session.Tick(0);
                        if(host.Session.Diagnostics.Count>0)throw new InvalidOperationException(stage.name+": "+string.Join("; ",host.Session.Diagnostics));
                        string preview=index==0?Root+"/Geometry/SceneFeatures":Root+"/Geometry/"+stage.name+"After";
                        SceneFeatureHostEditor.SavePreview(host,preview);
                        EditorUtility.SetDirty(host);
                        report.AppendLine(stage.name+": Day01 object/tool scale and terrain tile 1×1 saved");
                    }
                } finally {for(int i=0;i<view.Stages.Length;i++)view.Stages[i].gameObject.SetActive(active[i]);}
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Day02 scene save failed");
                return "SUCCESS: Day01 object and tool scale applied to Day02 stages 1–4. Play/Test Runner not run.\n"+report;
            } finally {if(opened&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
        }
        public static string Install()
        {
            string path=Root+"/Scenes/Devlog_Day02.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(!opened&&scene.isDirty)throw new InvalidOperationException("Day02 씬의 기존 변경을 먼저 저장하세요.");
            var after=Load<SceneFeatureProfile>("Assets/Modules/SceneFeatures/Presets/After.asset");
            if(after==null)throw new InvalidOperationException("After profile missing");
            Directory.CreateDirectory(Root+"/Data/IslandArt");AssetDatabase.Refresh();
            Directory.CreateDirectory("../../../devlog/youtube/day-02/source/archive");
            File.Copy(path,"../../../devlog/youtube/day-02/source/archive/before-all-islands-after.unity.txt",true);
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var report=new System.Text.StringBuilder();
            try {
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day02Showcase>(true)).Single();
                foreach(var host in view.Stages.Select(t=>t.GetComponent<SceneFeatureHost>()))host?.Clear();
                var wasActive=view.Stages.Select(t=>t.gameObject.activeSelf).ToArray();
                foreach(var stage in view.Stages)stage.gameObject.SetActive(false);
                try {
                    var decorations=CreateDecorations();
                    ConfigureClickIsland(view.Stages[0],after,decorations,report);
                    for(int index=1;index<4;index++)Build(view,index,after,decorations,report);
                } finally {for(int i=0;i<view.Stages.Length;i++)view.Stages[i].gameObject.SetActive(wasActive[i]);}
                view.ShowcaseCamera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Day02 scene save failed");
                Selection.activeObject=after;
                return "SUCCESS: Day02 islands 2–4 saved. Play/Test Runner not run.\n"+report;
            } finally {if(opened&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
        }
        private static void ConfigureClickIsland(Transform stage,SceneFeatureProfile after,GameObject[] decorations,System.Text.StringBuilder report)
        {
            var host=stage.GetComponent<SceneFeatureHost>();
            if(host==null)throw new InvalidOperationException("Click island feature host missing");
            string folder=Root+"/Data/IslandArt/ClickIslandAfter.asset";
            var profile=Load<SceneFeatureProfile>(folder);
            if(profile==null){profile=UnityEngine.Object.Instantiate(after);profile.name="ClickIslandAfter";AssetDatabase.CreateAsset(profile,folder);}
            profile.Settings=after.Settings.Copy();profile.DecorationPrefabs=decorations;
            profile.Settings.DecorationCount=14;profile.Settings.DecorationRadius=4;profile.Settings.PixelHeight=480;profile.Settings.OpenSeaFoam=.025f;
            profile.Settings.Set(SceneFeature.Decorations,true);
            profile.Settings.Set(SceneFeature.Pixels,true);
            profile.Settings.Set(SceneFeature.Vision,false);
            profile.Settings.Set(SceneFeature.VisionBlur,false);
            profile.Settings.Set(SceneFeature.Smoke,false);
            profile.Settings.Night=false;
            host.Profile=profile;
            host.Overrides.Values.Map.Radius=13;
            host.Overrides.Values.Map.GrassRadiusSquared=6.4;
            host.Overrides.Values.OpenSeaFoam=.025f;
            host.Overrides.Features=new[]{new FeatureOverride{Feature=SceneFeature.Vision,Enabled=false},
                new FeatureOverride{Feature=SceneFeature.VisionBlur,Enabled=false},
                new FeatureOverride{Feature=SceneFeature.Pixels,Enabled=true},
                new FeatureOverride{Feature=SceneFeature.Smoke,Enabled=false},
                new FeatureOverride{Feature=SceneFeature.Decorations,Enabled=true}};
            var map=Load<MapAsset>(Root+"/Data/IslandArt/ClickIslandMap.asset");
            if(map==null){map=ScriptableObject.CreateInstance<MapAsset>();AssetDatabase.CreateAsset(map,Root+"/Data/IslandArt/ClickIslandMap.asset");}
            map.Data=MapGenerator.Generate(host.Resolve().Map);FillMissingBeach(map.Data);
            host.Bindings.Map=map;profile.Settings.Set(SceneFeature.MapGeneration,false);
            EditorUtility.SetDirty(map);
            host.Apply();host.Session.SetVision(false);host.Session.Tick(0);
            if(host.Session.Diagnostics.Count>0)throw new InvalidOperationException(stage.name+": "+string.Join("; ",host.Session.Diagnostics));
            SavePreview(host,Root+"/Geometry/SceneFeatures");
            EditorUtility.SetDirty(profile);EditorUtility.SetDirty(host);
            report.AppendLine(stage.name+": Day01 decorations and pixel output, calm sea, vision/night off at start");
        }
        private static void Build(Day02Showcase view,int index,SceneFeatureProfile after,GameObject[] decorations,System.Text.StringBuilder report)
        {
            var stage=view.Stages[index];bool large=index==3;
            // A repeated command keeps the authored composition and replaces only its generated preview.
            bool first=stage.Find(Marker)==null;
            var resources=stage.GetComponentsInChildren<Day02Resource>(true);
            if(resources.Length!=(large?18:2))throw new InvalidOperationException("Unexpected resource count on "+stage.name);
            var host=stage.GetComponent<SceneFeatureHost>();if(host==null)host=stage.gameObject.AddComponent<SceneFeatureHost>();host.Clear();
            if(first) {
                var legacy=new GameObject("LegacyIslandGeometry");legacy.transform.SetParent(stage,false);
                foreach(var child in stage.Cast<Transform>().ToArray())
                    if(child!=legacy.transform && child.GetComponent<Day02Resource>()==null)child.SetParent(legacy.transform,true);
                legacy.SetActive(false);
                new GameObject(Marker).transform.SetParent(stage,false);
            }
            string folder=Root+"/Data/IslandArt/"+stage.name;
            var profile=Load<SceneFeatureProfile>(folder+".asset");
            if(profile==null){profile=UnityEngine.Object.Instantiate(after);profile.name=stage.name+"After";AssetDatabase.CreateAsset(profile,folder+".asset");}
            profile.Settings=after.Settings.Copy();profile.DecorationPrefabs=decorations;
            var s=profile.Settings;s.Set(SceneFeature.MapGeneration,false);s.Set(SceneFeature.Decorations,true);
            s.Set(SceneFeature.Pixels,true);s.Set(SceneFeature.Vision,false);s.Set(SceneFeature.VisionBlur,false);s.Set(SceneFeature.Smoke,large);
            s.Night=false;s.OpenSeaFoam=.025f;
            s.GrassSamples=large?6:7;s.GrassSize=large?.78f:.82f;s.LeafCards=large?900:1800;
            s.DecorationCount=large?60:index==2?20:14;s.DecorationRadius=large?7:4;s.WindStrength=.65f;s.PixelHeight=480;s.HitShakeStrength=.09f;
            s.Map.Seed=21+index;s.CursorRadius=large?3.4f:4;s.SourceRadius=large?3.5f:4;
            s.Palette[0]=index==1?new Color(.40f,.57f,.27f):index==2?new Color(.25f,.46f,.32f):new Color(.40f,.54f,.25f);
            var map=Load<MapAsset>(folder+"Map.asset");if(map==null){map=ScriptableObject.CreateInstance<MapAsset>();AssetDatabase.CreateAsset(map,folder+"Map.asset");}
            map.Data=IslandMap(index);s.Map.Radius=map.Data.Radius;
            var geometry=new TerrainGeometry(map.Data.Radius,map.Data.At,s.Palette,new CoastalTerrain(CoastShape.Rounded,s.WaterHeight,s.FloorHeight));
            var targets=new List<FeedbackTarget>();var picks=new List<PickSurface>();var leaves=new List<MeshFilter>();
            for(int n=0;n<resources.Length;n++) {
                var resource=resources[n];
                resource.GetComponent<FeedbackTarget>()?.ReleasePresentation();
                foreach(var cap in resource.Art.GetComponentsInChildren<PickSurface>(true).Where(p=>p.SnowCap).ToArray())UnityEngine.Object.DestroyImmediate(cap.gameObject);
                if(first) {
                    foreach(var child in resource.Art.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                    string model=resource.Stone?new[]{"Rock0_09","Rock1_10","Rock2_11","Rock3_12"}[n%4]:new[]{"Tree0_00","Tree1_01","Tree2_02","Tree3_03"}[n%4];
                    var modelObject=Art(Source+"/Resources/"+model+".prefab",resource.Art);
                    SyncCollider(resource,modelObject);
                    Vector3 p;
                    if(!large)p=resource.Stone?new Vector3(.72f,0,-.32f):new Vector3(-.72f,0,.18f);
                    else {int cluster=n/3;float a=cluster*Mathf.PI/3+.25f;var center=new Vector3(Mathf.Cos(a)*3.8f,0,Mathf.Sin(a)*3.8f);p=center+new[]{new Vector3(.58f,0,-.25f),new Vector3(-.62f,0,-.1f),new Vector3(0,0,.65f)}[n%3];}
                    p.y=geometry.Height(p.x,p.z);resource.transform.localPosition=p;
                }
                var target=resource.GetComponent<FeedbackTarget>();if(target==null)target=resource.gameObject.AddComponent<FeedbackTarget>();
                target.Visual=resource.Art;target.Wood=!resource.Stone;target.Juicy=true;target.ExternalTick=true;targets.Add(target);
                foreach(var filter in resource.Art.GetComponentsInChildren<MeshFilter>()) {
                    if(filter.sharedMesh==null||!filter.sharedMesh.isReadable)continue;
                    var pick=filter.GetComponent<PickSurface>();if(pick==null)pick=filter.gameObject.AddComponent<PickSurface>();
                    pick.VisibleRenderer=filter.GetComponent<Renderer>();pick.Target=target;picks.Add(pick);
                    if(!resource.Stone)leaves.Add(filter);
                }
                EditorUtility.SetDirty(resource);EditorUtility.SetDirty(target);
            }
            Transform house=null,chimney=null;Renderer[] windows=Array.Empty<Renderer>();
            if(large) {
                house=stage.Find("AfterHouse");
                if(house==null) {house=Art(Source+"/Environment/House_Pivot.prefab",stage).transform;house.name="AfterHouse";house.localPosition+=new Vector3(0,geometry.Height(0,.5f),.5f);}
                house.localRotation=Load<GameObject>(Source+"/Environment/House_Pivot.prefab").transform.localRotation;
                var houseBounds=BoundsOf(house.gameObject);
                house.position+=stage.TransformPoint(new Vector3(0,geometry.Height(0,.5f),.5f))
                    -new Vector3(houseBounds.center.x,houseBounds.min.y,houseBounds.center.z);
                windows=house.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.HasProperty("_HouseWindows"))).ToArray();
                chimney=house.Find("SmokeAnchor");
                if(chimney==null){chimney=new GameObject("SmokeAnchor").transform;chimney.SetParent(house,false);var bounds=BoundsOf(house.gameObject);chimney.position=new Vector3(bounds.center.x+.22f,bounds.max.y-.12f,bounds.center.z+.16f);}
                PlaceDay01Prop(stage,"Ruins_Pivot",new Vector3(-2.8f,0,.7f),geometry);
                PlaceDay01Prop(stage,"AbandonedAltar_Pivot",new Vector3(.1f,0,2.1f),geometry);
                PlaceDay01Prop(stage,"FallenLog_Pivot",new Vector3(-2,0,-3.1f),geometry);
            }
            host.Profile=profile;host.ExternalTick=true;host.ActiveEnvironment=true;host.Overrides=new SceneFeatureOverrides();
            host.Bindings=new SceneBindings {Camera=view.ShowcaseCamera,Sun=view.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).First(l=>l.type==LightType.Directional),Map=map,Targets=targets.ToArray(),FoliageSurfaces=leaves.ToArray(),PickSurfaces=picks.ToArray(),VisionSource=house,Windows=windows,BloomTargets=windows,SmokeEmitters=chimney!=null?new[]{chimney}:Array.Empty<Transform>()};
            // Automatic work uses one local tool per resource so a field can animate several nodes together.
            host.Bindings.Axe=resources.First(r=>!r.Stone).Tool;host.Bindings.Pick=resources.First(r=>r.Stone).Tool;
            host.Bindings.AxeContact=ContactAnchor(host.Bindings.Axe);host.Bindings.PickContact=ContactAnchor(host.Bindings.Pick);
            EditorUtility.SetDirty(profile);EditorUtility.SetDirty(map);
            host.Apply();host.Session.SetVision(false);host.Session.Tick(0);
            if(host.Session.Diagnostics.Count>0)throw new InvalidOperationException(stage.name+": "+string.Join("; ",host.Session.Diagnostics));
            SavePreview(host,Root+"/Geometry/"+stage.name+"After");
            EditorUtility.SetDirty(host);
            report.AppendLine(stage.name+": "+resources.Length+" resources, "+leaves.Count+" foliage surfaces, "+s.DecorationCount+" decorations, saved preview");
        }
        private static Transform ContactAnchor(GameObject tool)
        {
            if(tool==null)return null;var anchor=tool.transform.Find("ImpactPoint");if(anchor!=null)return anchor;
            anchor=new GameObject("ImpactPoint").transform;anchor.SetParent(tool.transform,false);return anchor;
        }
        private static void SavePreview(SceneFeatureHost host,string folder)
        {
            SceneFeatureHostEditor.SavePreview(host,folder);
            var keep=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var filter in host.BakedPreview.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null)keep.Add(AssetDatabase.GetAssetPath(filter.sharedMesh));
            foreach(var renderer in host.BakedPreview.GetComponentsInChildren<MeshRenderer>(true))
                foreach(var material in renderer.sharedMaterials)
                    if(material!=null)keep.Add(AssetDatabase.GetAssetPath(material));
            // Only this folder's generated preview assets are eligible; Unity removes their .meta files.
            foreach(var file in Directory.GetFiles(folder))
            {
                var name=Path.GetFileName(file);
                bool generated=System.Text.RegularExpressions.Regex.IsMatch(name,@"^(Mesh( [0-9]+)?\.asset|Material( [0-9]+)?\.mat)$");
                if(!generated)continue;
                var path=file.Replace('\\','/');
                if(!keep.Contains(path) && !AssetDatabase.DeleteAsset(path))throw new IOException("Cannot prune obsolete preview asset: "+path);
            }
        }
        private static void PlaceDay01Prop(Transform stage,string name,Vector3 position,TerrainGeometry geometry)
        {
            var placed=stage.Find("After"+name);
            if(placed==null){placed=Art(Source+"/Environment/"+name+".prefab",stage).transform;placed.name="After"+name;}
            position.y=geometry.Height(position.x,position.z);
            placed.localPosition=position;
        }
        private static MapData IslandMap(int index)
        {
            bool large=index==3;var map=new MapData(large?22:13);
            for(int z=-map.Radius;z<=map.Radius;z++)for(int x=-map.Radius;x<=map.Radius;x++) {
                double angle=Math.Atan2(z,x),r=Math.Sqrt(x*x/(large?1.12:1.22)+z*z/(large?.96:.94));
                double edge=(large?6.25:3.45)+.23*Math.Sin(angle*3+index)+.16*Math.Cos(angle*5);
                if(index==2&&x>1&&z<0)edge-=.8; // A visible cove distinguishes the field demonstration island.
                // The field island needs a full, clickable beach band for the ground-color demonstration.
                double beachWidth=large?.7:index==2?1.15:.55;
                var biome=r>edge?TerrainBiome.Water:r>edge-beachWidth?TerrainBiome.Sand:TerrainBiome.Grass;
                // A restrained worn path leads out from the house without covering resource clusters.
                if(large&&biome==TerrainBiome.Grass&&Math.Abs(x)<.6&&z<=0&&z>=-4)biome=TerrainBiome.Dirt;
                if(large&&biome==TerrainBiome.Grass&&z>=4&&x>=1&&x<=2)biome=TerrainBiome.Snow;
                map.Tiles[(z+map.Radius)*map.Size+x+map.Radius]=biome;
            }
            FillMissingBeach(map);
            return map;
        }
        private static void FillMissingBeach(MapData map)
        {
            // Inspect the original tiles once: add only missing sand outside grass, never grow from new sand.
            var missing=new HashSet<int>();
            for(int z=-map.Radius;z<=map.Radius;z++)for(int x=-map.Radius;x<=map.Radius;x++)
            {
                if(map.At(x,z)!=TerrainBiome.Grass)continue;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,nz=z+dz;
                    if(Math.Abs(nx)>map.Radius || Math.Abs(nz)>map.Radius)continue;
                    if(map.At(nx,nz)==TerrainBiome.Water)missing.Add((nz+map.Radius)*map.Size+nx+map.Radius);
                }
            }
            foreach(int index in missing)map.Tiles[index]=TerrainBiome.Sand;
        }
        private static void SetPrefabScale(GameObject go,GameObject prefab,Transform parent,bool center)
        {
            if(prefab==null)throw new InvalidOperationException("Day01 source prefab missing");
            go.transform.localScale=prefab.transform.localScale;
            if(!center)return;
            go.transform.localPosition=Vector3.zero;
            var bounds=BoundsOf(go);
            go.transform.position-=new Vector3(bounds.center.x-parent.position.x,bounds.min.y-parent.position.y,bounds.center.z-parent.position.z);
        }
        private static void SyncCollider(Day02Resource resource,GameObject model)
        {
            if(resource==null)return;var box=resource.GetComponent<BoxCollider>();if(box==null)return;
            var bounds=BoundsOf(model);box.center=resource.transform.InverseTransformPoint(bounds.center);
            box.size=bounds.size;EditorUtility.SetDirty(box);
        }
        private static void MatchDay01Tool(Day02Resource resource,float targetHeight)
        {
            var tool=resource.Tool;if(tool==null)throw new InvalidOperationException("Day02 tool missing: "+resource.name);
            var model=tool.transform.Cast<Transform>().FirstOrDefault(t=>t.GetComponentsInChildren<Renderer>(true).Length>0);
            if(model==null)throw new InvalidOperationException("Day02 tool model missing: "+resource.name);
            float current=BoundsOf(model.gameObject).size.y;
            if(current<.001f)throw new InvalidOperationException("Day02 tool has zero height: "+resource.name);
            model.localScale*=targetHeight/current;
            var modelBounds=BoundsOf(model.gameObject);
            model.position-=new Vector3(modelBounds.center.x-tool.transform.position.x,modelBounds.min.y-tool.transform.position.y,modelBounds.center.z-tool.transform.position.z);
            if(resource.Stone) {
                var p=tool.transform.localPosition;p.y=Mathf.Clamp(BoundsOf(resource.Art.gameObject).size.y*.55f,.28f,.58f);
                tool.transform.localPosition=p;
            }
        }
        private static GameObject Art(string path,Transform parent)
        {
            var prefab=Load<GameObject>(path);if(prefab==null)throw new FileNotFoundException(path);
            var go=UnityEngine.Object.Instantiate(prefab,parent,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
            foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
            foreach(var collider in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            SetPrefabScale(go,prefab,parent,true);
            return go;
        }
        private static Bounds BoundsOf(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new InvalidOperationException("Missing art renderer: "+go.name);
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);return bounds;
        }
        private static GameObject[] CreateDecorations()
        {
            var source=Load<GameObject>(Source+"/Environment/GroundContactDetails.prefab");if(source==null)throw new InvalidOperationException("Day01 ground details missing");
            var result=new List<GameObject>();
            foreach(string kind in new[]{"Fern","Pebbles"}) {
                string path=Root+"/Data/IslandArt/"+kind+".prefab";var saved=Load<GameObject>(path);
                if(saved==null) {
                    var sample=source.GetComponentsInChildren<Transform>(true).First(t=>t.name.EndsWith("_"+kind));
                    var go=UnityEngine.Object.Instantiate(sample.gameObject);go.name=kind;go.transform.position=Vector3.zero;
                    foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
                    try{saved=PrefabUtility.SaveAsPrefabAsset(go,path);}finally{UnityEngine.Object.DestroyImmediate(go);}
                }
                result.Add(saved);
            }
            return result.ToArray();
        }
    }
}
