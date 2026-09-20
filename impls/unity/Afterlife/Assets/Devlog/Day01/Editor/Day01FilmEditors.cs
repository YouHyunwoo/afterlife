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
            EditorGUILayout.HelpBox("기본: 1~6 쇼케이스 / F8 영상 시작 / Space 일시정지 / ← → 컷 이동 / Home 처음 / Esc 종료",MessageType.Info);
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
}
