using System;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Cinematics;
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
        [Serializable] private sealed class Shot {public string title;public int stage;public bool interactive;public float duration;public Cue[] cues;}
        [Serializable] private sealed class Cue {public float start,duration,audioSeconds;public string text,file;}
#pragma warning restore CS0649
        
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
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_AfterlifePrototype/Resources/Fonts/Mona12TextKR SDF.asset");
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
                view.Pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/_AfterlifePrototype/Resources/AfterlifePrototype/Pipeline.asset");
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
        private static GameObject Model(Transform parent,string name,Vector3 position,float height,Material material){var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/"+name+".fbx");if(source==null)throw new InvalidDataException("Missing art "+name);var go=UnityEngine.Object.Instantiate(source,parent);go.name=name;go.transform.localPosition=Vector3.zero;var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers){bounds.Encapsulate(renderer.bounds);renderer.sharedMaterial=material;}float scale=height/Mathf.Max(.01f,bounds.size.y);go.transform.localScale*=scale;var center=parent.InverseTransformPoint(bounds.center);go.transform.localPosition=position-new Vector3(center.x*scale,(center.y-bounds.size.y*.5f)*scale,center.z*scale);return go;}
        private static void Resource(Transform parent,Vector3 position,bool stone,int variant,Material palette){var root=new GameObject(stone?"StoneResource":"TreeResource");root.transform.SetParent(parent,false);root.transform.localPosition=position;var resource=root.AddComponent<Day02Resource>();resource.Stone=stone;var art=new GameObject("Art").transform;art.SetParent(root.transform,false);resource.Art=art;Model(art,(stone?"Rock":"Tree")+variant,Vector3.zero,stone?.85f:2f,palette);var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,stone?.45f:1,0);box.size=new Vector3(.9f,stone?.9f:2,.9f);var tool=new GameObject("ToolPivot");tool.transform.SetParent(root.transform,false);tool.transform.localPosition=new Vector3(.55f,stone?.45f:.8f,-.2f);Model(tool.transform,stone?"PickTool":"AxeTool",Vector3.zero,stone?.56f:.62f,palette);resource.Tool=tool;tool.SetActive(false);}
    }
}
