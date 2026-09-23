using Afterlife.Devlog.Cinematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Devlog.Day01
{
    // 입력·장면 전환만 맡는 에피소드 어댑터. 기본 모드는 항상 수동 쇼케이스다.
    [DefaultExecutionOrder(-100)]
    public sealed class Day01FilmDirector : MonoBehaviour, IFilmStageView, IFilmLightingView
    {
        public Day01Showcase Showcase;
        public ShowcaseFilm Film;
        private ShowcaseFilmPlayer _player;
        private int _returnStage;
        private bool _returnGuide, _returnShake;
        public bool IsFilmMode => _player != null;
        public bool Playing => _player != null && _player.Playing;
        public float Elapsed => _player != null ? _player.Time : 0;
        public float Duration => Film != null ? Film.Duration : 0;
        public int ShotIndex => _player != null ? _player.Index : 0;
        public Vector3 Origin => Showcase.StageOrigin;
        private void Update()
        {
            var keyboard=Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f8Key.wasPressedThisFrame) { if (IsFilmMode) StopFilm(); else StartFilm(); }
                if (IsFilmMode)
                {
                    if (keyboard.escapeKey.wasPressedThisFrame) StopFilm();
                    else if (keyboard.spaceKey.wasPressedThisFrame) TogglePause();
                    else if (keyboard.leftArrowKey.wasPressedThisFrame) Jump(-1);
                    else if (keyboard.rightArrowKey.wasPressedThisFrame) Jump(1);
                    else if (keyboard.homeKey.wasPressedThisFrame) Restart();
                }
            }
            _player?.Tick(Time.unscaledDeltaTime);
        }
        public void StartFilm()
        {
            if (!Application.isPlaying || IsFilmMode) return;
            if (Showcase==null || !Showcase.isActiveAndEnabled || !Showcase.HasPresentationStages || Film==null || Film.Shots==null || Film.Shots.Length==0 || Film.SubtitleFont==null)
            { Debug.LogError("Film requires the six-stage showcase, sequence and subtitle font.",this); return; }
            _returnStage=Showcase.CurrentStageIndex; _returnGuide=Showcase.ShowGuide; _returnShake=Showcase.CameraShake;
            Showcase.FilmMode=true; Showcase.ShowGuide=false; Showcase.CameraShake=false;
            try { _player=new ShowcaseFilmPlayer(Film,Showcase.ShowcaseCamera,this,transform); }
            catch (System.Exception exception) { EndFilm(); Debug.LogException(exception,this); }
        }
        public void StopFilm() { var player=_player; _player=null; player?.Dispose(); }
        public void TogglePause() => _player?.TogglePause();
        public void Jump(int direction) => _player?.Jump(direction);
        public void Restart() => _player?.Restart();
        public void Seek(float seconds) => _player?.Seek(seconds);
        public void EnterShot(FilmShot shot, bool reset)
        {
            if (Showcase.CurrentStageIndex!=shot.Stage || reset) Showcase.SetStage(shot.Stage);
            Showcase.ShowGuide=false; Showcase.FilmInteraction=shot.AllowInteraction;
            Showcase.FilmVision=shot.ShowVision;
            Showcase.SetFilmNight(shot.Night,reset);
            Showcase.SetFilmLook();
        }
        public void EndFilm()
        {
            if (Showcase==null) return;
            Showcase.FilmMode=false; Showcase.FilmInteraction=false;
            Showcase.FilmVision=false;Showcase.SetFilmLightingBlend(null);
            Showcase.ShowGuide=_returnGuide; Showcase.CameraShake=_returnShake;
            if (Showcase.isActiveAndEnabled) Showcase.SetStage(_returnStage);
        }
        private void OnDisable() { StopFilm(); }
        public void SetLightingBlend(float blend) => Showcase.SetFilmLightingBlend(blend);
    }
}
