using Afterlife.Devlog.Cinematics;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Afterlife.Devlog.Day03
{
    [DefaultExecutionOrder(-100)]
    public sealed class Day03FilmDirector : MonoBehaviour,IFilmStageView,IFilmLightingView,IFilmHudView
    {
        public Day03Showcase View;
        public ShowcaseFilm Film;
        private ShowcaseFilmPlayer _player;
        private bool _automatic, _waiting, _waitPaused, _restoreShot;
        private int _index;
        public Vector3 Origin=>View.Origin;
        private void Update()
        {
            var keys=Keyboard.current;
            if(keys!=null){
                if(keys.f8Key.wasPressedThisFrame){if(_player!=null)StopFilm();else StartFilm(false);}
                if(keys.f9Key.wasPressedThisFrame){if(_player!=null)StopFilm();else StartFilm(true);}
                if(_player!=null){
                    if(keys.escapeKey.wasPressedThisFrame)StopFilm();
                    else if(keys.homeKey.wasPressedThisFrame){_waiting=false;_waitPaused=false;View.ClearShotHistory();View.ResetLive();_player.Restart();}
                    else if(keys.spaceKey.wasPressedThisFrame){if(_waiting)_waitPaused=!_waitPaused;else _player.TogglePause();}
                    else if(keys.rightArrowKey.wasPressedThisFrame){SkipNextShot();return;}
                    else if(keys.leftArrowKey.wasPressedThisFrame){SkipPreviousShot();return;}
                }
            }
            if(_player==null)return;
            float boundary=0;for(int i=0;i<=_index;i++)boundary+=Film.Shots[i].Duration;
            float delta=Time.unscaledDeltaTime;
            if(!_waiting&&_player.Playing&&_player.Time+delta>=boundary&&!View.ContinuousShotReady(_index)){
                // Keep the actual economy running while a manual take needs more time.
                _player.TogglePause();_waiting=true;_waitPaused=false;
            }
            if(_waiting&&View.ContinuousShotReady(_index)){_waiting=false;_player.TogglePause();}
            View.Paused=_waiting?_waitPaused:!_player.Playing;
            _player.Tick(delta);
            if(_player.Time>=Film.Duration)StopFilm();
        }
        public void StartFilm(bool automatic)
        {
            if(!Application.isPlaying||_player!=null||View==null||Film==null||Film.SubtitleFont==null)return;
            _automatic=automatic;_waiting=false;_waitPaused=false;View.ClearShotHistory();View.ResetLive();View.FilmMode=true;
            _player=new ShowcaseFilmPlayer(Film,View.ShowcaseCamera,this,transform);
        }
        public void StopFilm(){var player=_player;_player=null;if(player!=null)player.Dispose();else EndFilm();}
        public void SkipNextShot()
        {
            if(_player==null)return;
            if(_index>=Film.Shots.Length-1){StopFilm();return;}
            bool playing=_waiting?!_waitPaused:_player.Playing;
            if(!View.AdvanceShotForSkip(Film.Shots[_index].Duration))return;
            _waiting=false;_waitPaused=false;
            _player.Jump(1);
            if(_player.Playing!=playing)_player.TogglePause();
            View.Paused=!playing;
        }
        public void SkipPreviousShot()
        {
            if(_player==null||!View.HasShotStart(Mathf.Max(0,_index-1)))return;
            bool playing=_waiting?!_waitPaused:_player.Playing;
            _waiting=false;_waitPaused=false;_restoreShot=true;
            try {_player.Jump(-1);}finally {_restoreShot=false;}
            if(_player.Playing!=playing)_player.TogglePause();
            View.Paused=!playing;
        }
        public void EnterShot(FilmShot shot,bool reset)
        {
            _index=System.Array.IndexOf(Film.Shots,shot);
            if(_restoreShot)View.RestoreShotStart(_index,_automatic);
            else View.EnterShot(_index,_automatic);
        }
        public void SetHudAlpha(float alpha)=>View.SetHudAlpha(alpha);
        public void SetLightingBlend(float blend)=>View.SetLightingBlend(blend);
        public void EndFilm(){_waiting=false;View?.EndFilm();}
        private void OnDisable()=>StopFilm();
    }
}
