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
        public bool ShowGuide = true;
        private ShowcaseFilmPlayer _player;
        private Day02Graphics _graphics;
        private bool _interactive;
        public Vector3 Origin => View.Origin;
        public bool IsFilmMode => _player != null;

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.f8Key.wasPressedThisFrame) { if (IsFilmMode) StopFilm(); else StartFilm(); }
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
            View.Paused = !_player.Playing; View.Interaction = _interactive; View.ShowGuide = ShowGuide;
            if (_player.Time >= Film.Duration) StopFilm();
        }

        public void StartFilm()
        {
            if (!Application.isPlaying || IsFilmMode || View == null || Film == null ||
                Film.SubtitleFont == null || Film.Shots == null || Film.Shots.Length == 0) return;
            View.FilmMode = true;
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
            _interactive = shot.AllowInteraction;
            View.Paused = false; View.Interaction = _interactive;
            _graphics.Show(shot);
        }
        public void EndFilm()
        {
            if (View != null) { View.FilmMode = false; View.Paused = false; View.Interaction = true; View.ShowGuide = ShowGuide; }
            _graphics?.Dispose(); _graphics = null;
        }
        private void OnDisable() => StopFilm();
    }
}
