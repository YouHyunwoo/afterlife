using Afterlife.Devlog.Cinematics;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    [CustomEditor(typeof(Day01FilmDirector))]
    internal sealed class Day01FilmDirectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var director=(Day01FilmDirector)target;
            if(director.Film!=null) FilmPresentationInspector.Draw(director.Film);
            EditorGUILayout.HelpBox("기본: 1~6 쇼케이스 / F8 자막·나레이션 영상 시작 / Space 영상·음성 일시정지 / ← → 컷 이동 / Home 처음 / Esc 종료",MessageType.Info);
            using(new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if(GUILayout.Button(director.IsFilmMode ? "쇼케이스로 돌아가기" : "영상 모드 시작"))
                {if(director.IsFilmMode) director.StopFilm(); else director.StartFilm();}
                if(director.IsFilmMode)
                {
                    EditorGUILayout.LabelField($"컷 {director.ShotIndex+1} | {director.Elapsed:0.0} / {director.Duration:0.0}초");
                    if(GUILayout.Button(director.Playing ? "연출 일시정지" : "연출 계속")) director.TogglePause();
                    float time=EditorGUILayout.Slider("시간 이동",director.Elapsed,0,director.Duration);
                    if(Mathf.Abs(time-director.Elapsed)>.01f) director.Seek(time);
                }
            }
            if(Application.isPlaying) Repaint();
        }
    }
    [CustomEditor(typeof(ShowcaseFilm))]
    internal sealed class ShowcaseFilmEditor : UnityEditor.Editor
    {
        private int _shot;
        public override void OnInspectorGUI()
        {
            var film=(ShowcaseFilm)target;
            EditorGUILayout.LabelField("총 길이",film.Duration.ToString("0.0")+"초");
            FilmPresentationInspector.Draw(film);
            EditorGUILayout.HelpBox("Cues가 있으면 문장별 Start/Duration/Text/Narration을 사용한다. Start는 해당 컷 안의 시간이다. Narration Volume으로 목소리 크기를 조정한다. 음원 길이보다 컷을 짧게 만들면 끝부분이 잘린다.",MessageType.Info);
            EditorGUILayout.HelpBox("컷별 Stage는 0~5(키 1~6). From/To는 공간 기준 좌표다. 컷 경계는 컷 전환, 컷 안에서는 부드럽게 이동한다. 재생 중 시간 이동은 파괴 이력을 되감지 않고 초기화한다.",MessageType.Info);
            if(film.Shots!=null && film.Shots.Length>0)
            {
                var names=new string[film.Shots.Length];
                for(int i=0;i<names.Length;i++) names[i]=(i+1)+". "+film.Shots[i].Title;
                _shot=EditorGUILayout.Popup("구도 편집 컷",Mathf.Clamp(_shot,0,names.Length-1),names);
                using(new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    if(GUILayout.Button("선택 컷을 Scene 뷰에서 보기")) ScenePose(film,false,false);
                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button("Scene 구도 → From")) ScenePose(film,true,false);
                    if(GUILayout.Button("Scene 구도 → To")) ScenePose(film,true,true);
                    EditorGUILayout.EndHorizontal();
                }
            }
            DrawDefaultInspector();
        }
        private void ScenePose(ShowcaseFilm film,bool capture,bool end)
        {
            var view=SceneView.lastActiveSceneView;
            var showcase=Object.FindFirstObjectByType<Day01Showcase>();
            if(view==null || showcase==null || !showcase.HasPresentationStages) {Debug.LogWarning("Open Day01 and a Scene view first.");return;}
            var shot=film.Shots[_shot]; var origin=showcase.StageRoots[shot.Stage].position;
            if(!capture) {var pose=shot.From; view.LookAt(origin+pose.Focus,Quaternion.LookRotation(-pose.Offset),pose.OrthographicSize,true);view.Repaint();return;}
            Undo.RecordObject(film,"촬영 구도 저장");
            var captured=new FilmPose(view.pivot-origin,view.camera.orthographic ? view.camera.orthographicSize : view.size);
            captured.Offset=view.rotation*Vector3.back*23;
            if(end) shot.To=captured; else shot.From=captured;
            EditorUtility.SetDirty(film); AssetDatabase.SaveAssets();
        }
    }
    internal static class FilmPresentationInspector
    {
        public static void Draw(ShowcaseFilm film)
        {
            var settings=new SerializedObject(film);
            settings.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("배경음악 · 크레딧",EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(settings.FindProperty("NarrationEnabled"),new GUIContent("나레이션 재생"));
            EditorGUILayout.PropertyField(settings.FindProperty("BackgroundMusicEnabled"),new GUIContent("배경음악 재생"));
            EditorGUILayout.HelpBox("효과음만 녹화하려면 두 항목을 끄세요. 자막·크레딧·게임 효과음은 유지됩니다.",MessageType.Info);
            EditorGUILayout.PropertyField(settings.FindProperty("BackgroundMusic"),new GUIContent("배경음악"));
            EditorGUILayout.Slider(settings.FindProperty("MusicVolume"),0,1,new GUIContent("배경음악 볼륨", "0.10 = 10%. 재생 중에도 반영됩니다."));
            EditorGUILayout.Slider(settings.FindProperty("CreditsMusicMultiplier"),0,4,new GUIContent("크레딧 음악 볼륨 배율", "기본 음악 볼륨 × 배율. 0.10 × 1.5 = 0.15. 크레딧 시작 2초간 전환합니다."));
            EditorGUILayout.Slider(settings.FindProperty("CreditsScale"),.5f,2.5f,new GUIContent("크레딧 크기 배율", "1 = 기존 크기. 제목·이름·여백을 같은 비율로 확대합니다."));
            settings.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("연결된 Film 에셋을 편집합니다. 볼륨과 크레딧 배율은 재생 중에도 반영되며, Play 중 에셋 수정도 유지됩니다.",MessageType.Info);
        }
    }
}
