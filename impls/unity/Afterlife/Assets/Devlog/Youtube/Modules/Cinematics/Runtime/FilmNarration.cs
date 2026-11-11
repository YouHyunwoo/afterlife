using System;
using UnityEngine;

namespace Afterlife.Devlog.Cinematics
{
    // 한 소스만 소유하며 컷/문장 전환·탐색·정지를 같은 영상 시각으로 맞춘다.
    // 게임 효과음의 AudioListener/전역 볼륨/시간 배율은 수정하지 않는다.
    public sealed class FilmNarration : IDisposable
    {
        private readonly AudioSource _source;
        private FilmCue _cue;
        private bool _running;

        public FilmNarration(Transform parent)
        {
            var go=new GameObject("FilmNarration"); go.transform.SetParent(parent,false);
            _source=go.AddComponent<AudioSource>();
            _source.playOnAwake=false; _source.loop=false; _source.spatialBlend=0;
            _source.priority=0; _source.dopplerLevel=0; _source.pitch=1;
            _source.ignoreListenerPause=true;
        }

        public void Sync(FilmCue cue, float shotTime, bool playing, float volume, bool discontinuity)
        {
            if (_cue!=cue || discontinuity)
            {
                _source.Stop(); _running=false; _cue=cue;
                _source.clip=cue!=null ? cue.Narration : null;
            }
            var clip=_source.clip;
            float local=cue==null ? -1 : shotTime-cue.Start;
            bool active=playing && clip!=null && local>=0 && local<clip.length;
            _source.volume=Mathf.Clamp01(volume);
            if (!active)
            {
                if (_running) _source.Pause();
                _running=false; return;
            }
            int sample=Mathf.Clamp(Mathf.RoundToInt(local*clip.frequency),0,clip.samples-1);
            if (!_running)
            {
                _source.timeSamples=sample; _source.Play(); _running=true;
            }
            else if (Mathf.Abs(_source.timeSamples-sample)>clip.frequency*.18f)
            {
                // 에디터 지연이나 스크럽 이후에도 음성이 영상보다 앞서지 않게 보정한다.
                _source.timeSamples=sample;
            }
        }

        public void Dispose()
        {
            if (_source==null) return;
            _source.Stop(); UnityEngine.Object.Destroy(_source.gameObject);
        }
    }
}
