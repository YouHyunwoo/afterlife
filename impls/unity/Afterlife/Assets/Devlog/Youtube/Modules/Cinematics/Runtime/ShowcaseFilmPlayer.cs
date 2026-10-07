using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Devlog.Cinematics
{
    // 시간 모델은 외부에서 Tick한다. 입력과 Day01 장면의 구체 타입은 알지 못한다.
    public sealed class ShowcaseFilmPlayer : IDisposable
    {
        private readonly ShowcaseFilm _film;
        private readonly Camera _camera;
        private readonly IFilmStageView _view;
        private readonly SequenceClock _clock;
        private readonly GameObject _canvas;
        private readonly RectTransform _frame;
        private readonly CanvasGroup _group;
        private readonly TextMeshProUGUI _subtitle;
        private readonly Image _fade;
        private readonly FilmNarration _narration;
        private readonly FilmNarration _music;
        private readonly FilmCue _musicCue;
        private readonly TextMeshProUGUI _heading;
        private readonly FilmCredits _credits;
        private readonly bool _orthographic;
        private readonly float _size, _fov;
        private int _index=-1;
        private int _fadeEntryFrame=-1;
        public int Index => _clock.Index;
        public float Time => (float)_clock.Time;
        public float Duration => (float)_clock.Duration;
        public bool Playing => _clock.Playing;
        public string Title => _film.Shots[Index].Title;
        public ShowcaseFilmPlayer(ShowcaseFilm film, Camera camera, IFilmStageView view, Transform parent)
        {
            _film=film; _camera=camera; _view=view;
            _clock=new SequenceClock(film.Shots.Select(s=>(double)s.Duration).ToArray());
            _orthographic=camera.orthographic; _size=camera.orthographicSize; _fov=camera.fieldOfView;
            camera.orthographic=true;
            _canvas=new GameObject("FilmSubtitles",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            _canvas.transform.SetParent(parent,false);
            var canvas=_canvas.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=2000;
            canvas.targetDisplay=camera.targetDisplay;
            var scaler=_canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            var frame=new GameObject("VideoSafeFrame",typeof(RectTransform)); frame.transform.SetParent(_canvas.transform,false);
            _frame=frame.GetComponent<RectTransform>();
            _heading=CreateCaption("FilmHeading",frame.transform,film,new Vector2(.055f,.88f),new Vector2(.80f,.95f),28,TextAlignmentOptions.MidlineLeft);
            if(!string.IsNullOrWhiteSpace(film.NarrationDisclosure))
            {
                var disclosure=CreateCaption("NarrationDisclosure",frame.transform,film,new Vector2(.55f,.94f),new Vector2(.95f,.985f),18,TextAlignmentOptions.MidlineRight);
                disclosure.text=film.NarrationDisclosure;
            }
            var panel=new GameObject("SubtitlePanel",typeof(RectTransform),typeof(CanvasGroup),typeof(Image));
            panel.transform.SetParent(frame.transform,false);
            var rect=panel.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(.09f,.055f); rect.anchorMax=new Vector2(.91f,.165f); rect.offsetMin=rect.offsetMax=Vector2.zero;
            panel.GetComponent<Image>().color=new Color(.025f,.04f,.05f,.76f); panel.GetComponent<Image>().raycastTarget=false;
            _group=panel.GetComponent<CanvasGroup>(); _group.blocksRaycasts=false; _group.interactable=false;
            var label=new GameObject("SubtitleText",typeof(RectTransform),typeof(TextMeshProUGUI)); label.transform.SetParent(panel.transform,false);
            _subtitle=label.GetComponent<TextMeshProUGUI>(); _subtitle.font=film.SubtitleFont;
            _subtitle.fontSize=film.SubtitleSize; _subtitle.alignment=TextAlignmentOptions.Center;
            _subtitle.color=Color.white; _subtitle.raycastTarget=false; _subtitle.richText=false;
            var textRect=_subtitle.rectTransform; textRect.anchorMin=Vector2.zero; textRect.anchorMax=Vector2.one;
            textRect.offsetMin=new Vector2(24,10); textRect.offsetMax=new Vector2(-24,-10);
            _credits=new FilmCredits(frame.transform,camera,film);
            // 크레딧보다 뒤에 생성해 최종 암전이 크레딧까지 덮게 한다.
            var fade=new GameObject("ShotFade",typeof(RectTransform),typeof(Image));
            fade.transform.SetParent(frame.transform,false);
            _fade=fade.GetComponent<Image>(); _fade.color=Color.clear; _fade.raycastTarget=false;
            var fadeRect=fade.GetComponent<RectTransform>();
            fadeRect.anchorMin=Vector2.zero; fadeRect.anchorMax=Vector2.one;
            fadeRect.offsetMin=fadeRect.offsetMax=Vector2.zero;
            _narration=new FilmNarration(parent);
            _music=new FilmNarration(parent);
            _musicCue=new FilmCue { Start=0, Duration=film.Duration, Narration=film.BackgroundMusic };
            _clock.SetPlaying(true); Evaluate(true);
            // 첫 출력 전에 새 Overlay의 크기와 정렬을 확정한다.
            Canvas.ForceUpdateCanvases();
        }
        public void Tick(float delta)
        {
            if (_fadeEntryFrame >= 0)
            {
                // 생성과 같은 프레임, 그리고 그 작업 시간을 보고하는 다음 프레임은 진행하지 않는다.
                // 새 화면이 실제로 출력되기 전에 초기화 비용으로 짧은 컷이나 Fade In이 끝나는 것을 막는다.
                delta=0;
                if (UnityEngine.Time.frameCount > _fadeEntryFrame) _fadeEntryFrame=-1;
            }
            _clock.Tick(delta); Evaluate(false);
        }
        public void TogglePause() { _clock.SetPlaying(!Playing); Evaluate(false); }
        public void Seek(float seconds) { bool playing=Playing; _clock.Seek(seconds); _clock.SetPlaying(playing); Evaluate(true); }
        public void Jump(int direction) { Seek((float)_clock.Start(Mathf.Clamp(Index+direction,0,_film.Shots.Length-1))); }
        public void Restart() { _clock.Seek(0); _clock.SetPlaying(true); Evaluate(true); }
        private void Evaluate(bool discontinuity)
        {
            int index=Index; var shot=_film.Shots[index];
            bool changed=_index!=index || discontinuity;
            if (changed)
            {
                _view.EnterShot(shot,discontinuity); _index=index; _subtitle.text=shot.Subtitle;
                _heading.text=shot.Heading;
                // Hard cuts also need a presented frame before initialization time advances the clock.
                _fadeEntryFrame=UnityEngine.Time.frameCount;
            }
            float local=(float)_clock.LocalTime;
            if (_view is IFilmLightingView lighting)
                lighting.SetLightingBlend(shot.TransitionToNight ? Mathf.SmoothStep(0,1,Mathf.Clamp01((local-shot.NightTransitionStart)/Mathf.Max(.01f,shot.NightTransitionDuration))) : (shot.Night ? 1 : 0));
            _fade.color=new Color(0,0,0,FadeAlpha(shot,local));
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(local/shot.Duration));
            Vector3 focus=Vector3.Lerp(shot.From.Focus,shot.To.Focus,t)+_view.Origin;
            Vector3 offset=Vector3.Lerp(shot.From.Offset,shot.To.Offset,t);
            if (offset.sqrMagnitude<.01f) offset=new Vector3(10,13,-16);
            _camera.transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset));
            _camera.orthographicSize=Mathf.Max(.1f,Mathf.Lerp(shot.From.OrthographicSize,shot.To.OrthographicSize,t));
            FilmCue activeCue=null;
            if (shot.Cues!=null)
                foreach (var cue in shot.Cues)
                    if (cue!=null && local>=cue.Start && local<cue.Start+cue.Duration) {activeCue=cue;break;}
            bool hasCues=shot.Cues!=null && shot.Cues.Length>0;
            string text=hasCues ? (activeCue!=null ? activeCue.Text : "") : shot.Subtitle;
            if (_subtitle.text!=text) _subtitle.text=text;
            float delay=activeCue!=null ? activeCue.Start : shot.SubtitleDelay;
            float age=local-delay;
            float end=Mathf.Min(activeCue!=null ? activeCue.Duration : shot.SubtitleDuration,shot.Duration-delay);
            _group.alpha=string.IsNullOrWhiteSpace(text) ? 0 : Mathf.Clamp01(age/.12f)*Mathf.Clamp01((end-age)/.18f);
            _narration.Sync(activeCue,local,Playing && _fadeEntryFrame<0 && _film.NarrationEnabled,_film.NarrationVolume,changed);
            float musicFade=Mathf.Clamp01(Time/2f)*Mathf.Clamp01((Duration-Time)/3f);
            float creditsGain=shot.ShowCredits
                ? Mathf.Lerp(1,Mathf.Clamp(_film.CreditsMusicMultiplier,0,4),Mathf.SmoothStep(0,1,Mathf.Clamp01(local/2f)))
                : 1;
            bool musicChanged=_musicCue.Narration!=_film.BackgroundMusic;
            _musicCue.Narration=_film.BackgroundMusic;
            _music.Sync(_musicCue,Time,Playing && _fadeEntryFrame<0 && _film.BackgroundMusicEnabled,Mathf.Clamp01(_film.MusicVolume*creditsGain)*musicFade,discontinuity || musicChanged);
            // 레터박스 안의 16:9 영상 영역에 자막을 고정한다.
            float h=Mathf.Min(Screen.height,Screen.width/(16f/9));
            Vector2 half=new Vector2(h*(16f/9)/Mathf.Max(1,Screen.width),h/Mathf.Max(1,Screen.height))*.5f;
            _frame.anchorMin=Vector2.one*.5f-half; _frame.anchorMax=Vector2.one*.5f+half; _frame.offsetMin=_frame.offsetMax=Vector2.zero;
            _credits.Evaluate(shot.ShowCredits,local,shot.Duration);
            if(shot.ShowCredits) {_group.alpha=0;_heading.text="";}
        }
        private static TextMeshProUGUI CreateCaption(string name,Transform parent,ShowcaseFilm film,Vector2 min,Vector2 max,float size,TextAlignmentOptions alignment)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            var label=go.GetComponent<TextMeshProUGUI>(); label.font=film.SubtitleFont;
            label.fontSize=size; label.alignment=alignment; label.color=Color.white;
            label.richText=false; label.raycastTarget=false; label.fontStyle=FontStyles.Bold;
            var rect=label.rectTransform; rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero;
            return label;
        }
        private static float FadeAlpha(FilmShot shot,float local)
        {
            float duration=Mathf.Max(.01f,shot.Duration);
            float fadeIn=shot.FadeIn ? Mathf.Max(.01f,shot.FadeInDuration) : 0;
            float fadeOut=shot.FadeOut ? Mathf.Max(.01f,shot.FadeOutDuration) : 0;
            // 페이드 총 길이가 컷보다 길면 비율을 유지해 맞춘다. 두 구간이 겹쳐 화면이 계속 어두워지는 것을 방지한다.
            float scale=Mathf.Min(1,duration/Mathf.Max(.01f,fadeIn+fadeOut));
            fadeIn*=scale; fadeOut*=scale;
            float entering=fadeIn>0 ? 1-Mathf.SmoothStep(0,1,Mathf.Clamp01(local/fadeIn)) : 0;
            float leaving=fadeOut>0 ? Mathf.SmoothStep(0,1,Mathf.Clamp01((local-(duration-fadeOut))/fadeOut)) : 0;
            return Mathf.Max(entering,leaving);
        }

        public void Dispose()
        {
            _narration.Dispose();
            _music.Dispose();
            _credits.Dispose();
            _canvas.SetActive(false); UnityEngine.Object.Destroy(_canvas);
            if (_camera!=null) { _camera.orthographic=_orthographic; _camera.orthographicSize=_size; _camera.fieldOfView=_fov; }
            _view.EndFilm();
        }
    }
}
