using System;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Cinematics;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterlife.Devlog.Day01.Editor
{
    [InitializeOnLoad]
    internal static class Day01FilmSetup
    {
        public const string FilmPath=Day01SceneBuilder.Root+"/Data/Day01Film.asset";
        private static bool _running;
        static Day01FilmSetup()
        {
            EditorApplication.delayCall+=Apply;
            EditorSceneManager.sceneSaved+=_=>EditorApplication.delayCall+=Apply;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall+=Apply;};
        }
        private static void Apply()
        {
            if(_running || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) {EditorApplication.delayCall+=Apply; return;}
            if(!File.Exists(Day01SceneBuilder.ScenePath)) return;
            var scene=SceneManager.GetSceneByPath(Day01SceneBuilder.ScenePath);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if(!opened && scene.isDirty) return;
            _running=true; var previous=SceneManager.GetActiveScene();
            try
            {
                if(opened) scene=EditorSceneManager.OpenScene(Day01SceneBuilder.ScenePath,OpenSceneMode.Additive);
                var showcase=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Day01Showcase>(true)).FirstOrDefault();
                if(showcase==null || !showcase.HasPresentationStages) return;
                var director=showcase.GetComponent<Day01FilmDirector>();
                if(director!=null && director.Film!=null && director.Showcase==showcase) return;
                var film=AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(FilmPath);
                if(film==null)
                {
                    if(!AssetDatabase.IsValidFolder(Day01SceneBuilder.Root+"/Data")) AssetDatabase.CreateFolder(Day01SceneBuilder.Root,"Data");
                    film=ScriptableObject.CreateInstance<ShowcaseFilm>(); film.name="Day01Film";
                    film.SubtitleFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_AfterlifePrototype/Resources/Fonts/Mona12TextKR SDF.asset");
                    if(film.SubtitleFont==null) throw new InvalidOperationException("Korean subtitle font missing.");
                    var after=showcase.StageRoots[5];
                    var tree=after.GetComponentsInChildren<ShowcaseTarget>(true).Where(t=>t.Wood).OrderBy(t=>t.transform.position.z).First();
                    var rock=after.GetComponentsInChildren<ShowcaseTarget>(true).Where(t=>!t.Wood).OrderBy(t=>t.transform.position.z).First();
                    var treeFocus=after.InverseTransformPoint(tree.transform.position)+Vector3.up;
                    var rockFocus=after.InverseTransformPoint(rock.transform.position)+Vector3.up*.4f;
                    Vector3 wide=new Vector3(0,.5f,0),water=new Vector3(3.8f,0,-3.6f),forest=new Vector3(-3,1,1.5f),snow=new Vector3(-2.5f,1.1f,4),house=new Vector3(2.35f,1,2.1f),crystal=new Vector3(-2.8f,.8f,.7f);
                    film.Shots=new[] {
                        Shot("완성본 도입",5,8,wide,5.5f,5.1f,"회색 도형으로 시작한 작은 세계입니다."),
                        Shot("Before",0,12,wide,5.5f,5.5f,"처음에는 나무도 집도 단순한 도형이었습니다."),
                        Shot("모델과 픽셀",1,20,wide,5.5f,5.5f,"중세 판타지 모델과 색을 입히고, 픽셀 느낌을 더했습니다."),
                        Shot("지형",2,12,wide,5.5f,4.8f,"반듯한 타일에 작은 굴곡과 자연스러운 경계를 더했습니다."),
                        Shot("해변과 물",2,13,water,2.6f,2.3f,"물에는 깊이와 거품을, 해변에는 부드러운 모서리를 만들었습니다."),
                        Shot("숲과 바람",3,13,forest,3.1f,2.8f,"풀과 나뭇잎이 바람에 흔들리기 시작합니다."),
                        Shot("눈",3,10,snow,2.8f,2.5f,"눈은 위를 향한 면에 쌓이고, 잎 사이로 원래 색이 보입니다."),
                        Shot("굴뚝",3,7,house,3,2.7f,"작은 굴뚝에도 움직임을 더했습니다."),
                        Shot("지면 호버 — 직접 조작",4,9,new Vector3(1,0,-1.7f),3.8f,3.8f,"마우스가 지나간 자리에도 반응이 남습니다.",false,true),
                        Shot("나무 타격 — 직접 조작",4,14,treeFocus,2.7f,2.7f,"도끼가 닿은 곳에 자국이 남고, 나무는 반대 방향으로 흔들립니다.",false,true),
                        Shot("바위 타격 — 직접 조작",4,14,rockFocus,2.5f,2.5f,"바위는 조각으로 흩어지고, 천천히 사라집니다.",false,true),
                        Shot("집과 밤",5,13,house,3.7f,3.3f,"밤에는 작은 불빛 주변만 보이도록 했습니다.",true),
                        Shot("수정",5,8,crystal,2.5f,2.2f,"어둠 속에서 빛나는 작은 발견도 남겨두었습니다.",true),
                        Shot("전후 비교 — Before",0,5,wide,5.5f,5.5f,"시작은 이 모습이었지만…"),
                        Shot("전후 비교 — After",5,7,wide,5.5f,5.5f,"이제 조금은, 만지고 싶은 세계가 되었습니다.")
                    };
                    AssetDatabase.CreateAsset(film,FilmPath);
                }
                if(director==null) director=showcase.gameObject.AddComponent<Day01FilmDirector>();
                director.Showcase=showcase; director.Film=film;
                EditorUtility.SetDirty(director); PrefabUtility.RecordPrefabInstancePropertyModifications(director);
                if(PrefabUtility.SaveAsPrefabAsset(showcase.gameObject,Day01SceneBuilder.Root+"/Prefabs/Composition/Day01Progression.prefab")==null)
                    throw new IOException("Film composition save failed.");
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Film scene save failed.");
                File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../devlog/youtube/records/movie-sequence-unity.json")),
                    "{\"savedAtUtc\":\""+DateTime.UtcNow.ToString("O")+"\",\"shots\":"+film.Shots.Length+",\"duration\":"+film.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"defaultMode\":\"showcase\",\"fontAssigned\":true}");
                Debug.Log("Devlog film saved: "+film.Shots.Length+" shots / "+film.Duration+" seconds. F8 starts film; default remains showcase.");
            }
            catch(Exception e) {Debug.LogException(e);}
            finally
            {
                if(opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene,true);
                if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                _running=false;
            }
        }
        private static FilmShot Shot(string title,int stage,float duration,Vector3 focus,float from,float to,string text,bool night=false,bool interaction=false)
        {return new FilmShot {Title=title,Stage=stage,Duration=duration,From=new FilmPose(focus,from),To=new FilmPose(focus,to),Subtitle=text,Night=night,AllowInteraction=interaction,SubtitleDuration=Mathf.Min(7,duration-1)};}
    }
}
