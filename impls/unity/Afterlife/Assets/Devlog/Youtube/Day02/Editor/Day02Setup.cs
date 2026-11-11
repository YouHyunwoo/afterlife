using System;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Cinematics;
using Afterlife.Modules.SceneFeatures;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace Afterlife.Devlog.Day02.Editor
{
    internal static class Day02Setup
    {
        private const string Root="Assets/Devlog/Youtube/Day02";
        private const string ScenePath=Root+"/Scenes/Devlog_Day02.unity";
#pragma warning disable CS0649
        [Serializable] private sealed class Manifest {public int version;public Shot[] shots;}
        [Serializable] private sealed class Shot {public string title,explanation;public int stage,workAmount,maxWorks,treeWorks;public bool interactive,credits,automatic,quickCut,closeUp,fixedTarget,teaser,hideHeading,fadeIn,fadeOut;public float duration,demoStart,fadeInDuration,fadeOutDuration;public Cue[] cues;}
        [Serializable] private sealed class Cue {public float start,duration,audioSeconds;public string text,file;}
#pragma warning restore CS0649

        public static string InstallHoldFilm()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;
            if(!opened&&scene.isDirty)throw new InvalidOperationException("Save the existing Day02 scene changes first.");
            var manifest=JsonUtility.FromJson<Manifest>(AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/Day02Narration.json").text);
            if(manifest.version!=3&&manifest.version!=4)throw new InvalidDataException("Expected Day02 narration version 3 or 4.");
            var film=AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(Root+"/Data/Day02Film.asset");
            var shots=manifest.shots.Select(s=>new FilmShot {
                Title=s.title,Heading=s.hideHeading||s.teaser?"":s.title,Stage=s.stage,Duration=s.duration,AllowInteraction=s.interactive,
                From=new FilmPose(Vector3.up*.6f,3.8f),To=new FilmPose(Vector3.up*.6f,3.8f),FadeIn=s.fadeIn||(!s.credits&&!s.quickCut),FadeOut=s.fadeOut||(!s.credits&&!s.quickCut),
                FadeInDuration=s.fadeInDuration>0?s.fadeInDuration:.6f,FadeOutDuration=s.fadeOutDuration>0?s.fadeOutDuration:.6f,
                ShowCredits=s.credits,
                Cues=s.cues.Select(c=> {
                    var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Narration/"+c.file);
                    if(clip==null||string.IsNullOrWhiteSpace(c.text)||c.start+c.duration>s.duration+.01f||Mathf.Abs(clip.length-c.audioSeconds)>.08f)
                        throw new InvalidDataException("Narration cue mismatch: "+c.file);
                    return new FilmCue{Start=c.start,Duration=c.duration,Text=c.text,Narration=clip};
                }).ToArray()
            }).ToArray();
            Directory.CreateDirectory("../../../devlog/youtube/day-02/source/archive");
            string archive="../../../devlog/youtube/day-02/source/archive/before-hold-film.unity.txt";
            if(!File.Exists(archive))File.Copy(ScenePath,archive);
            if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try {
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day02Showcase>(true)).Single();
                // The existing small After island is the reusable composition for comparisons.
                if(view.Stages[2].name!="EfficiencyIsland") {
                    var source=view.Stages[1].GetComponent<SceneFeatureHost>();
                    var sun=source.Bindings.Sun;
                    Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
                    var template=UnityEngine.Object.Instantiate(view.Stages[1].gameObject);
                    GameObject prefab;
                    try {
                        template.name="SmallHarvestIsland";template.transform.position=Vector3.zero;
                        var host=template.GetComponent<SceneFeatureHost>();host.Bindings.Camera=null;host.Bindings.Sun=null;
                        prefab=PrefabUtility.SaveAsPrefabAsset(template,Root+"/Prefabs/SmallHarvestIsland.prefab");
                        if(prefab==null)throw new IOException("Island prefab save failed.");
                    } finally {UnityEngine.Object.DestroyImmediate(template);}
                    for(int i=1;i<4;i++) {
                        var old=view.Stages[i];
                        var copy=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                        copy.name=i==1?"HoldIsland":i==2?"EfficiencyIsland":"GoldIsland";
                        var host=copy.GetComponent<SceneFeatureHost>();host.Bindings.Camera=view.ShowcaseCamera;host.Bindings.Sun=sun;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(host);
                        view.Stages[i]=copy.transform;
                        UnityEngine.Object.DestroyImmediate(old.gameObject);
                    }
                }
                view.Stages[1].name="HoldIsland";
                for(int i=0;i<4;i++) {
                    view.Stages[i].position=new Vector3(i*40,0,0);
                    view.Stages[i].gameObject.SetActive(true);
                }
                view.SceneVersion=4;view.RingMaterial=null;view.Demonstration=false;view.ShowGuide=false;
                EnsureClickIslandStone(view);
                view.GoldHudSprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Prototype/Resources/UI/Icons16.png").OfType<Sprite>().Single(s=>s.name=="Icons16_5");
                var director=view.GetComponent<Day02FilmDirector>();
                director.ShowGuide=false;
                director.ShotWorkAmounts=manifest.shots.Select(s=>Math.Max(1,s.workAmount)).ToArray();
                director.ShotPresentations=manifest.shots.Select(s=>new Day02ShotPresentation {
                    Automatic=s.automatic,FixedTarget=s.fixedTarget,Teaser=s.teaser,MaxWorks=s.maxWorks,TreeWorks=s.treeWorks,DemoStart=s.demoStart,Explanation=s.explanation
                }).ToArray();
                for(int i=0;i<shots.Length;i++) {
                    var source=manifest.shots[i];
                    // Gold uses the wider reward composition even when its demo target is fixed.
                    if(!source.closeUp&&(!source.fixedTarget||source.stage!=2))continue;
                    var tree=view.Stages[source.stage].GetComponentsInChildren<Day02Resource>(true).First(r=>!r.Stone);
                    var focus=tree.transform.position-view.Stages[source.stage].position+Vector3.up*.7f;
                    shots[i].From=shots[i].To=new FilmPose(focus,2.8f);
                }
                EditorUtility.SetDirty(director);
                view.ShowcaseCamera.transform.SetPositionAndRotation(new Vector3(10,13.6f,-16),Quaternion.LookRotation(new Vector3(-10,-13,16)));
                view.ShowcaseCamera.orthographicSize=3.8f;
                film.Shots=shots;film.NarrationVersion=manifest.version;
                ConfigureCredits(film);
                EditorUtility.SetDirty(film);EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Day02 save failed.");
                AssetDatabase.SaveAssets();
                return "SUCCESS: Day02 film v"+manifest.version+", "+shots.Length+" shots, "+film.Duration+" seconds. Four small islands at X=0,40,80,120. Play not run.";
            } finally {if(opened&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
        }

        private static void EnsureClickIslandStone(Day02Showcase view)
        {
            var stage=view.Stages[0];var host=stage.GetComponent<SceneFeatureHost>();
            var stone=stage.GetComponentsInChildren<Day02Resource>(true).FirstOrDefault(r=>r.Stone);
            if(stone==null) {
                var source=view.Stages[1].GetComponentsInChildren<Day02Resource>(true).First(r=>r.Stone);
                string path=Root+"/Prefabs/HarvestStone.prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab==null) {
                    var template=UnityEngine.Object.Instantiate(source.gameObject);
                    try {
                        template.name="HarvestStone";template.transform.position=Vector3.zero;
                        template.GetComponent<Day02Resource>().Tool=null;
                        prefab=PrefabUtility.SaveAsPrefabAsset(template,path);
                    } finally {UnityEngine.Object.DestroyImmediate(template);}
                }
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,stage);
                instance.name="StoneResource";stone=instance.GetComponent<Day02Resource>();
                var tree=stage.GetComponentsInChildren<Day02Resource>(true).First(r=>!r.Stone);
                instance.transform.localPosition=new Vector3(.72f,tree.transform.localPosition.y,-.32f);
                if(host.Bindings.Map!=null) {
                    var map=host.Bindings.Map.Data;var settings=host.Resolve();
                    var coast=new Afterlife.Modules.Terrain.CoastalTerrain(
                        settings.IsEnabled(Afterlife.Modules.SceneFeatures.SceneFeature.Coast)?settings.CoastShape:Afterlife.Modules.Terrain.CoastShape.Square,
                        settings.WaterHeight,settings.FloorHeight);
                    var geometry=new Afterlife.Modules.TerrainView.TerrainGeometry(map.Radius,map.At,settings.Palette,coast,
                        map.UseHeightSamples?(Func<float,float,float>)((x,z)=>(float)map.Height(x,z)):null);
                    var position=instance.transform.localPosition;position.y=geometry.Height(position.x,position.z);
                    instance.transform.localPosition=position;
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            }
            if(host.Bindings.Pick==null) {
                var source=view.Stages[1].GetComponent<SceneFeatureHost>().Bindings;
                string contactPath=AnimationUtility.CalculateTransformPath(source.PickContact,source.Pick.transform);
                string path=Root+"/Prefabs/HarvestPick.prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab==null) {
                    var template=UnityEngine.Object.Instantiate(source.Pick);
                    try {
                        template.name="HarvestPick";template.transform.position=Vector3.zero;
                        prefab=PrefabUtility.SaveAsPrefabAsset(template,path);
                    } finally {UnityEngine.Object.DestroyImmediate(template);}
                }
                var pick=(GameObject)PrefabUtility.InstantiatePrefab(prefab,stage);
                pick.name="PickTool";pick.SetActive(false);
                host.Bindings.Pick=pick;host.Bindings.PickContact=pick.transform.Find(contactPath);
                PrefabUtility.RecordPrefabInstancePropertyModifications(pick);
            }
            host.Bindings.Targets=stage.GetComponentsInChildren<FeedbackTarget>(true);
            host.Bindings.PickSurfaces=host.Bindings.PickSurfaces.Concat(
                stone.GetComponentsInChildren<Afterlife.Modules.Interaction.PickSurface>(true)).Distinct().ToArray();
            EditorUtility.SetDirty(host);
        }

        // Copies only the authored preset. Runtime uses the shared Cinematics module.
        private static void ConfigureCredits(ShowcaseFilm film)
        {
            var source=AssetDatabase.LoadAssetAtPath<ShowcaseFilm>("Assets/Devlog/Youtube/Day01/Data/Day01Film.asset");
            if(source==null)throw new InvalidDataException("Day01 credits preset missing.");
            var preset=source.Shots.Last(s=>s.ShowCredits);
            var content=film.Shots.Where(s=>!s.ShowCredits).ToArray();
            if(content.Length==0)throw new InvalidDataException("Film content missing.");
            var last=content.Last();last.FadeOut=false;
            film.Shots=content.Concat(new[]{new FilmShot {
                Title=preset.Title,Heading="",Stage=last.Stage,Duration=preset.Duration,
                From=last.To,To=last.To,AllowInteraction=false,ShowCredits=true,
                FadeIn=preset.FadeIn,FadeInDuration=preset.FadeInDuration,
                FadeOut=preset.FadeOut,FadeOutDuration=preset.FadeOutDuration,
                Cues=Array.Empty<FilmCue>()
            }}).ToArray();
            film.NarrationDisclosure="";
            film.CreditsText=source.CreditsText;
            film.CreditsBlurShader=source.CreditsBlurShader;
            film.CreditsFadeSeconds=source.CreditsFadeSeconds;
            film.CreditsDarkness=source.CreditsDarkness;
            film.CreditsBlurRadius=source.CreditsBlurRadius;
            film.CreditsScale=source.CreditsScale;
            film.BackgroundMusic=source.BackgroundMusic;
            film.MusicVolume=source.MusicVolume;
            film.CreditsMusicMultiplier=source.CreditsMusicMultiplier;
        }

        public static string InstallCredits()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var film=AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(Root+"/Data/Day02Film.asset");
            if(film==null)throw new InvalidDataException("Day02 film missing.");
            if(EditorUtility.IsDirty(film))throw new InvalidOperationException("Save existing film changes first.");
            Undo.RecordObject(film,"Apply Day01 ending credits to Day02");
            ConfigureCredits(film);EditorUtility.SetDirty(film);AssetDatabase.SaveAssets();
            return "SUCCESS: Day02 credits saved; "+film.Shots.Length+" shots, "+film.Duration+" seconds. Play not run.";
        }

        // Timing-only edits preserve camera poses, credits, music and scene bindings.
        public static string UpdateFilmTiming()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var manifest=JsonUtility.FromJson<Manifest>(AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/Day02Narration.json").text);
            var film=AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(Root+"/Data/Day02Film.asset");
            if((manifest.version!=3&&manifest.version!=4) || film==null || manifest.shots.Length!=film.Shots.Length)
                throw new InvalidDataException("Film/manifest shot count mismatch.");
            for(int i=0;i<film.Shots.Length;i++) {
                var source=manifest.shots[i];var target=film.Shots[i];
                if(source.stage!=target.Stage || source.interactive!=target.AllowInteraction || source.credits!=target.ShowCredits || source.cues.Length!=target.Cues.Length)
                    throw new InvalidDataException("Film structure mismatch: "+i);
                for(int j=0;j<source.cues.Length;j++) {
                    var cue=source.cues[j];var existing=target.Cues[j];
                    var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Narration/"+cue.file);
                    if(clip==null || existing.Narration!=clip || cue.text!=existing.Text ||
                        Mathf.Abs(cue.start-existing.Start)>.001f || Mathf.Abs(cue.duration-existing.Duration)>.001f ||
                        cue.start+cue.duration>source.duration+.01f || Mathf.Abs(clip.length-cue.audioSeconds)>.08f)
                        throw new InvalidDataException("Narration timing mismatch: "+cue.file);
                }
            }
            Undo.RecordObject(film,"Shorten Day02 harvest demonstrations");
            for(int i=0;i<film.Shots.Length;i++)film.Shots[i].Duration=manifest.shots[i].duration;
            EditorUtility.SetDirty(film);AssetDatabase.SaveAssets();
            return "SUCCESS: timing saved; "+film.Shots.Length+" shots, "+film.Duration+" seconds. Play not run.";
        }

        private static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Install;return;}
            if(File.Exists(ScenePath)&&File.ReadAllText(ScenePath).Contains("SceneVersion: 3"))
            {
                if(AssetDatabase.IsValidFolder(Root+"/Data/Day02SettingsData"))AssetDatabase.DeleteAsset(Root+"/Data/Day02SettingsData");
                return;
            }
            var json=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/Day02Narration.json");
            if(json==null)return;
            var manifest=JsonUtility.FromJson<Manifest>(json.text);if(manifest.version!=2)return;
            var shader=Shader.Find("Afterlife/Devlog/Day02Palette");if(shader==null)return;
            var old=SceneManager.GetSceneByPath(ScenePath);
            if(old.IsValid()&&old.isDirty){Debug.LogWarning("Day02 rebuild is waiting: save or close the modified Day02 scene.");return;}
            var previous=SceneManager.GetActiveScene();Scene scene=default;
            try {
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Prototype/Resources/Fonts/Mona12TextKR SDF.asset");
                if(font==null)throw new InvalidDataException("Korean font missing");
                var film=AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(Root+"/Data/Day02Film.asset");
                if(film==null){film=ScriptableObject.CreateInstance<ShowcaseFilm>();AssetDatabase.CreateAsset(film,Root+"/Data/Day02Film.asset");}
                film.Shots=manifest.shots.Select(s=>new FilmShot{
                    Title=s.title,Heading=s.title,Stage=s.stage,Duration=s.duration,AllowInteraction=s.interactive,
                    From=new FilmPose(Vector3.up*.6f,s.stage==3?6.5f:3.2f),To=new FilmPose(Vector3.up*.6f,s.stage==3?6.5f:3.2f),
                    FadeIn=true,FadeOut=true,Cues=s.cues.Select(c=>{
                        var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Narration/"+c.file);
                        if(clip==null||string.IsNullOrWhiteSpace(c.text)||c.start+c.duration>s.duration+.01f||Mathf.Abs(clip.length-c.audioSeconds)>.08f)throw new InvalidDataException(c.file);
                        return new FilmCue{Start=c.start,Duration=c.duration,Text=c.text,Narration=clip};
                    }).ToArray()
                }).ToArray();
                film.SubtitleFont=font;film.NarrationVersion=2;film.BackgroundMusicEnabled=false;film.NarrationDisclosure="AI 생성 나레이션 · Supertonic 3 · M2";EditorUtility.SetDirty(film);
                Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh();
                var palette=Material("Palette",Color.white,true,shader);
                var grass=Material("Grass",new Color(.30f,.49f,.24f),false,shader);
                var sand=Material("Sand",new Color(.67f,.57f,.35f),false,shader);
                var water=Material("Water",new Color(.10f,.32f,.40f),false,shader);
                var waterLight=Material("WaterLight",new Color(.12f,.37f,.44f),false,shader);
                var ring=Material("SpiritRing",new Color(.45f,1,.85f),false,Shader.Find("Universal Render Pipeline/Unlit"));
                if(File.Exists(ScenePath) && !File.Exists("../../../devlog/youtube/day-02/source/archive/prototype-based-scene.unity.txt")) {Directory.CreateDirectory("../../../devlog/youtube/day-02/source/archive");File.Copy(ScenePath,"../../../devlog/youtube/day-02/source/archive/prototype-based-scene.unity.txt",true);}
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
                var root=new GameObject("Day02Showcase");var view=root.AddComponent<Day02Showcase>();view.Font=font;view.RingMaterial=ring;
                view.GoldHudSprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Prototype/Resources/UI/Icons16.png").OfType<Sprite>().Single(s=>s.name=="Icons16_5");
                view.Pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Prototype/Resources/AfterlifePrototype/Pipeline.asset");
                if(view.Pipeline==null)throw new InvalidDataException("Pipeline missing");
                var cameraObject=new GameObject("ShowcaseCamera",typeof(Camera),typeof(AudioListener));var camera=cameraObject.GetComponent<Camera>();
                camera.orthographic=true;camera.orthographicSize=3.2f;camera.backgroundColor=new Color(.09f,.22f,.29f);camera.clearFlags=CameraClearFlags.SolidColor;camera.farClipPlane=150;
                camera.transform.SetPositionAndRotation(new Vector3(10,13.6f,-16),Quaternion.LookRotation(new Vector3(-10,-13,16)));view.ShowcaseCamera=camera;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                var sun=new GameObject("Sun",typeof(Light));sun.transform.rotation=Quaternion.Euler(45,-30,0);sun.GetComponent<Light>().type=LightType.Directional;sun.GetComponent<Light>().intensity=1.2f;
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.7f,.75f);
                view.Stages=new Transform[4];
                for(int stage=0;stage<4;stage++){
                    var space=new GameObject(new[]{"ClickIsland","HoverIsland","FieldIsland","GoldIsland"}[stage]).transform;space.position=new Vector3(stage*40,0,0);view.Stages[stage]=space;
                    int extent=stage==3?10:6;float radius=stage==3?6.4f:2.8f;
                    for(int z=-extent;z<=extent;z++)for(int x=-extent;x<=extent;x++){
                        float distance=Mathf.Sqrt(x*x+z*z);bool land=distance<radius;
                        Tile(space,"WaterTile",new Vector3(x,-.32f,z),new Vector3(.99f,.10f,.99f),(x+z)%3==0?waterLight:water);
                        if(land){Tile(space,"ShoreTile",new Vector3(x,-.15f,z),new Vector3(.98f,.26f,.98f),sand);Tile(space,"GrassTile",new Vector3(x,.005f,z),new Vector3(.93f,.10f,.93f),grass);}
                    }
                    if(stage<2){Resource(space,new Vector3(-.85f,.06f,0),false,0,palette);Resource(space,new Vector3(.85f,.06f,0),true,0,palette);}
                    else if(stage==2){Resource(space,new Vector3(-.65f,.06f,0),false,1,palette);Resource(space,new Vector3(.65f,.06f,0),true,1,palette);}
                    else {
                        for(int i=0;i<18;i++){float a=i*2.39996f;float r=2.4f+(i%3)*1.1f;Resource(space,new Vector3(Mathf.Cos(a)*r,.06f,Mathf.Sin(a)*r),i%3==0,i%4,palette);}
                        Model(space,"House",new Vector3(0,.06f,0),2,palette);
                    }
                    space.gameObject.SetActive(stage==0);
                }
                var director=root.AddComponent<Day02FilmDirector>();director.View=view;director.Film=film;
                if(old.IsValid()&&old.isLoaded)EditorSceneManager.CloseScene(old,true);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Scene save failed");
                AssetDatabase.SaveAssets();
                // 폐기한 프로토타입 설정과 이전 음성 임포트본은 Unity API로 제거한다.
                AssetDatabase.DeleteAsset(Root+"/Data/Day02Settings.asset");
                var used=manifest.shots.SelectMany(s=>s.cues).Select(c=>c.file).ToArray();
                foreach(var path in Directory.GetFiles(Root+"/Audio/Narration","*.wav"))if(!used.Contains(Path.GetFileName(path)))AssetDatabase.DeleteAsset(path.Replace('\\','/'));
                File.WriteAllText("../../../devlog/youtube/day-02/records/editor-assets.md","# Day02 빈 씬 재구성\n\nUnity NewScene(EmptyScene)에서 4개 섬 공간, 카메라, 조명, 자원과 도구, 독립 HUD, 필름을 저장했다. PrototypeScene/View와 본편 UI 없음. 9샷, "+film.Duration+"초. Play 미실행.\n");
                Debug.Log("Day02 empty-scene rebuild v2 saved: four islands, no prototype UI.");
            } catch(Exception error){Debug.LogException(error);}
            finally {if(previous.IsValid()&&previous.isLoaded){SceneManager.SetActiveScene(previous);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}}
        }
        private static Material Material(string name,Color color,bool vertex,Shader shader){var path=Root+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;mat=new Material(shader){name=name};mat.SetColor("_BaseColor",color);if(mat.HasProperty("_VertexColors"))mat.SetFloat("_VertexColors",vertex?1:0);AssetDatabase.CreateAsset(mat,path);return mat;}
        private static void Tile(Transform parent,string name,Vector3 position,Vector3 scale,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());}
        private static GameObject Model(Transform parent,string name,Vector3 position,float height,Material material){var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototype/Resources/AfterlifePrototype/World/"+name+".fbx");if(source==null)throw new InvalidDataException("Missing art "+name);var go=UnityEngine.Object.Instantiate(source,parent);go.name=name;go.transform.localPosition=Vector3.zero;var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers){bounds.Encapsulate(renderer.bounds);renderer.sharedMaterial=material;}float scale=height/Mathf.Max(.01f,bounds.size.y);go.transform.localScale*=scale;var center=parent.InverseTransformPoint(bounds.center);go.transform.localPosition=position-new Vector3(center.x*scale,(center.y-bounds.size.y*.5f)*scale,center.z*scale);return go;}
        private static void Resource(Transform parent,Vector3 position,bool stone,int variant,Material palette){var root=new GameObject(stone?"StoneResource":"TreeResource");root.transform.SetParent(parent,false);root.transform.localPosition=position;var resource=root.AddComponent<Day02Resource>();resource.Stone=stone;var art=new GameObject("Art").transform;art.SetParent(root.transform,false);resource.Art=art;Model(art,(stone?"Rock":"Tree")+variant,Vector3.zero,stone?.85f:2f,palette);var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,stone?.45f:1,0);box.size=new Vector3(.9f,stone?.9f:2,.9f);var tool=new GameObject("ToolPivot");tool.transform.SetParent(root.transform,false);tool.transform.localPosition=new Vector3(.55f,stone?.45f:.8f,-.2f);Model(tool.transform,stone?"PickTool":"AxeTool",Vector3.zero,stone?.56f:.62f,palette);resource.Tool=tool;tool.SetActive(false);}
    }
}
