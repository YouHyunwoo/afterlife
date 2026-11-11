using System;
using UnityEngine;
using TMPro;

namespace Afterlife.Devlog.Cinematics
{
    [Serializable]
    public struct FilmPose
    {
        public Vector3 Focus;
        public Vector3 Offset;
        [Min(.1f)] public float OrthographicSize;
        public FilmPose(Vector3 focus, float size)
        { Focus=focus; Offset=new Vector3(10,13,-16); OrthographicSize=size; }
    }
    [Serializable]
    public sealed class FilmCue
    {
        [Min(0)] public float Start;
        [Min(.1f)] public float Duration=5;
        [TextArea(2,4)] public string Text;
        public AudioClip Narration;
    }
    [Serializable]
    public sealed class FilmShot
    {
        public string Title;
        [Range(0,5)] public int Stage;
        [Min(.1f)] public float Duration=8;
        public FilmPose From=new FilmPose(Vector3.up*.5f,6.5f);
        public FilmPose To=new FilmPose(Vector3.up*.5f,6.5f);
        [TextArea(2,4)] public string Subtitle;
        [Min(0)] public float SubtitleDelay=.4f;
        [Min(.1f)] public float SubtitleDuration=7;
        [Header("Shot transition")]
        public bool FadeIn;
        [Min(.01f)] public float FadeInDuration=.6f;
        public bool FadeOut;
        [Min(.01f)] public float FadeOutDuration=.6f;
        public bool Night;
        public bool AllowInteraction;
        public string Heading;
        public FilmCue[] Cues=Array.Empty<FilmCue>();
        public bool ShowCredits;
        public bool ShowVision;
        public bool TransitionToNight;
        public float NightTransitionStart=7;
        public float NightTransitionDuration=3;
    }
    public sealed class ShowcaseFilm : ScriptableObject
    {
        public FilmShot[] Shots=Array.Empty<FilmShot>();
        public TMP_FontAsset SubtitleFont;
        [Range(20,72)] public float SubtitleSize=38;
        [Range(0,1)] public float NarrationVolume=.85f;
        public bool NarrationEnabled=true;
        public bool BackgroundMusicEnabled=true;
        public AudioClip BackgroundMusic;
        [Range(0,1)] public float MusicVolume=.08f;
        [Tooltip("크레딧에서 기본 배경음악 볼륨에 곱하는 배율. 진입 2초 동안 부드럽게 적용합니다.")]
        [Range(0,4)] public float CreditsMusicMultiplier=1.5f;
        public string NarrationDisclosure;
        [Tooltip("크레딧 직전 샷의 끝에서 제목과 IFilmHudView HUD가 사라지는 시간. 0이면 기존 즉시 숨김을 유지합니다.")]
        [Min(0)] public float CreditsHudFadeSeconds;
        public Shader CreditsBlurShader;
        [Min(.1f)] public float CreditsFadeSeconds=2;
        [Range(0,1)] public float CreditsDarkness=.38f;
        [Range(0,12)] public float CreditsBlurRadius=5;
        [TextArea(4,12)] public string CreditsText;
        [Range(.5f,2.5f)] public float CreditsScale=1.25f;
        [HideInInspector] public int NarrationVersion;
        public float Duration { get { float total=0; foreach(var shot in Shots) if(shot!=null) total+=shot.Duration; return total; } }
    }
    public interface IFilmStageView
    {
        Vector3 Origin { get; }
        void EnterShot(FilmShot shot, bool reset);
        void EndFilm();
    }
    public interface IFilmHudView
    {
        void SetHudAlpha(float alpha);
    }
    public interface IFilmLightingView
    {
        void SetLightingBlend(float blend);
    }
}
