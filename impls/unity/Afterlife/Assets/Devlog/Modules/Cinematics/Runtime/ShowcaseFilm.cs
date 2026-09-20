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
    }
    public sealed class ShowcaseFilm : ScriptableObject
    {
        public FilmShot[] Shots=Array.Empty<FilmShot>();
        public TMP_FontAsset SubtitleFont;
        [Range(20,72)] public float SubtitleSize=38;
        public float Duration { get { float total=0; foreach(var shot in Shots) if(shot!=null) total+=shot.Duration; return total; } }
    }
    public interface IFilmStageView
    {
        Vector3 Origin { get; }
        void EnterShot(FilmShot shot, bool reset);
        void EndFilm();
    }
}
