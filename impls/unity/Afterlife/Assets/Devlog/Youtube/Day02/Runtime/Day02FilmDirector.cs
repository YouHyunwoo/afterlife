using Afterlife.Devlog.Cinematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Devlog.Day02
{
    [DefaultExecutionOrder(-100)]
    public sealed class Day02FilmDirector : MonoBehaviour, IFilmStageView
    {
        public Day02Showcase View;
        public ShowcaseFilm Film;
        public int[] ShotWorkAmounts = System.Array.Empty<int>();
        public Day02ShotPresentation[] ShotPresentations = System.Array.Empty<Day02ShotPresentation>();
        public bool ShowGuide;
        private ShowcaseFilmPlayer _player;
        private Day02Graphics _graphics;
        private bool _interactive;
        private bool _creditsActive;
        private bool _automaticPreview;
        public Vector3 Origin => View.Origin;
        public bool IsFilmMode => _player != null;

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.f8Key.wasPressedThisFrame) { if (IsFilmMode) StopFilm(); else { View.Demonstration=false; StartFilm(); } }
                if (keys.f9Key.wasPressedThisFrame) { if(IsFilmMode) StopFilm(); else { View.Demonstration=true; StartFilm(); } }
                if (keys.hKey.wasPressedThisFrame) ShowGuide = !ShowGuide;
                if (IsFilmMode)
                {
                    if (keys.escapeKey.wasPressedThisFrame) StopFilm();
                    else if (keys.spaceKey.wasPressedThisFrame) _player.TogglePause();
                    else if (keys.leftArrowKey.wasPressedThisFrame) _player.Jump(-1);
                    else if (keys.rightArrowKey.wasPressedThisFrame) _player.Jump(1);
                    else if (keys.homeKey.wasPressedThisFrame) _player.Restart();
                }
            }
            if (_player == null) { if(View!=null) View.ShowGuide=ShowGuide; return; }
            _player.Tick(Time.unscaledDeltaTime);
            View.Paused = !_player.Playing; View.Interaction = _interactive; View.ShowGuide = ShowGuide&&!_creditsActive;
            if (_player.Time >= Film.Duration) StopFilm();
        }

        public void StartFilm()
        {
            if (!Application.isPlaying || IsFilmMode || View == null || Film == null ||
                Film.SubtitleFont == null || Film.Shots == null || Film.Shots.Length == 0) return;
            View.FilmMode = true;
            _automaticPreview=View.Demonstration;
            try
            {
                _graphics = new Day02Graphics(transform, Film.SubtitleFont);
                _player = new ShowcaseFilmPlayer(Film, View.ShowcaseCamera, this, transform);
            }
            catch (System.Exception error) { EndFilm(); Debug.LogException(error, this); }
        }
        public void StopFilm()
        {
            var player = _player; _player = null;
            if (player != null) player.Dispose(); else EndFilm();
        }
        public void EnterShot(FilmShot shot, bool reset)
        {
            View.SetStage(shot.Stage);
            View.WorkAmount=shot.Stage>=2?3:1;
            int index=System.Array.IndexOf(Film.Shots,shot);
            if(index>=0&&index<ShotWorkAmounts.Length)View.WorkAmount=Mathf.Max(1,ShotWorkAmounts[index]);
            var direction=index>=0&&index<ShotPresentations.Length?ShotPresentations[index]:null;
            View.Demonstration=_automaticPreview||(direction!=null&&direction.Automatic);
            View.DemoWorkLimit=direction!=null?direction.MaxWorks:0;
            View.DemoFixedTarget=direction!=null&&direction.FixedTarget;
            View.DemoStart=direction!=null?direction.DemoStart:0;
            View.DemoTreeWorks=direction!=null?direction.TreeWorks:0;
            _interactive = shot.AllowInteraction;
            _creditsActive=shot.ShowCredits;
            View.Paused = false; View.Interaction = _interactive;
            View.ShowGuide=ShowGuide&&!_creditsActive;
            _graphics.Show(shot,direction);
        }
        public void EndFilm()
        {
            _creditsActive=false;_automaticPreview=false;
            if(View!=null){View.DemoWorkLimit=0;View.DemoFixedTarget=false;View.DemoStart=0;View.DemoTreeWorks=0;}
            if (View != null) { View.Demonstration=false; View.FilmMode = false; View.Paused = false; View.Interaction = true; View.ShowGuide = ShowGuide; }
            _graphics?.Dispose(); _graphics = null;
        }
        private void OnDisable() => StopFilm();
    }
}
