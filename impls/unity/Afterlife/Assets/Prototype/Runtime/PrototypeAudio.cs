using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Prototype
{
    // 모델 이벤트만 구독하는 씬 수명 오디오 출력. 게임 난수와 오디오 난수를 분리한다.
    public sealed partial class PrototypeAudio : MonoBehaviour
    {
        private sealed class Cue
        {
            public AudioClip[] Clips;
            public float Volume, Cooldown, NextTime;
            public bool Important;
            public float PitchVariation;
            public bool Sequential;
            public int NextClip;
        }
        private struct Request { public SoundCue Cue; public float Pan; }
        private readonly Dictionary<SoundCue, Cue> _cues = new Dictionary<SoundCue, Cue>();
        private readonly Dictionary<SoundCue, Request> _pending = new Dictionary<SoundCue, Request>();
        private readonly System.Random _random = new System.Random();
        [SerializeField] private AudioSource[] _voices = new AudioSource[12];
        private VillageModel _model;
        private PrototypeSettings _settings;
        private Camera _camera;
        private int _feedbackVoice;

        public bool Initialize(VillageModel model, PrototypeSettings settings, Camera camera)
        {
            if (_model != null) return true;
            if (camera == null || camera.GetComponent<AudioListener>() == null ||
                _voices == null || _voices.Length != 12 || System.Array.Exists(_voices, voice => voice == null))
            {
                Debug.LogError("Prototype audio references are incomplete: assign the camera AudioListener and all 12 prefab voice sources.", this);
                return false;
            }
            _model = model; _settings = settings; _camera = camera;
            for (int i = 0; i < _voices.Length; i++)
            {
                var source = _voices[i];
                source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
                source.dopplerLevel = 0; source.priority = i < 8 ? 160 : 64;
            }
            foreach(var entry in settings.Audio!=null?settings.Audio.Cues:PrototypeAudioSettings.Defaults())
            {
                var clips=new List<AudioClip>();
                if(entry.Clips!=null)foreach(var clip in entry.Clips)if(clip!=null)clips.Add(clip);
                if(clips.Count==0 && entry.ResourcePaths!=null)foreach(var path in entry.ResourcePaths){var clip=Resources.Load<AudioClip>(path);if(clip!=null)clips.Add(clip);}
                _cues.Add(entry.Id,new Cue { Clips=clips.ToArray(),Volume=entry.Volume,Cooldown=entry.Cooldown,Important=entry.Important,Sequential=entry.Sequential,PitchVariation=entry.PitchVariation });
            }
            InitializeEnvironmentAudio();
            _model.SoundRequested += RequestSound;
            _model.SoundReset += ResetSounds;
            RequestSound(SoundCue.Day);
            return true;
        }

        public void RequestSound(SoundCue cue, Point? position = null)
        {
            if (!isActiveAndEnabled || !_cues.TryGetValue(cue, out var definition) || definition.Clips.Length == 0) return;
            if (_pending.ContainsKey(cue) || Time.unscaledTime < definition.NextTime) return;
            float pan = 0;
            if (position.HasValue)
            {
                var point = position.Value;
                // 화면 밖/안개 속 전투음을 숨겨서 적 위치가 소리로 유출되지 않게 한다.
                if (_model.Visibility(point) <= .12f) return;
                var screen = _camera.WorldToViewportPoint(new Vector3(point.X, .5f, point.Z));
                if (screen.z <= 0 || screen.x < 0 || screen.x > 1 || screen.y < 0 || screen.y > 1) return;
                pan = Mathf.Clamp((screen.x - .5f) * .8f, -.4f, .4f);
            }
            // 같은 프레임의 다중 대상 기여/전투는 큐 종류당 한 번으로 합친다.
            if (!_pending.ContainsKey(cue)) _pending.Add(cue, new Request { Cue = cue, Pan = pan });
        }

        private void LateUpdate()
        {
            int worldStarts = 0;
            foreach (var request in _pending.Values)
            {
                var cue = _cues[request.Cue];
                if (Time.unscaledTime < cue.NextTime) continue;
                int slot = -1;
                if (cue.Important)
                {
                    for (int i = 8; i < _voices.Length; i++) if (!_voices[i].isPlaying) { slot = i; break; }
                    if (slot < 0) slot = 8 + _feedbackVoice++ % 4;
                }
                else
                {
                    if (worldStarts >= 4) continue;
                    int matching = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (!_voices[i].isPlaying) { if (slot < 0) slot = i; continue; }
                        foreach (var clip in cue.Clips) if (_voices[i].clip == clip) matching++;
                    }
                    if (slot < 0 || matching >= 2) continue;
                    worldStarts++;
                }
                var source = _voices[slot];
                source.Stop(); source.clip = cue.Clips[cue.Sequential ? cue.NextClip++ % cue.Clips.Length : _random.Next(cue.Clips.Length)];
                source.volume = Mathf.Clamp01(_settings.SoundVolume) * cue.Volume;
                source.panStereo = request.Pan; source.pitch = 1 + Mathf.Sin(Time.unscaledTime*7)*cue.PitchVariation; source.Play();
                cue.NextTime = Time.unscaledTime + cue.Cooldown;
            }
            _pending.Clear();
        }

        private void ResetSounds()
        {
            _pending.Clear();
            _environmentAmbience?.Reset();_environmentWater?.Reset();
            foreach (var source in _voices) if (source != null) source.Stop();
            foreach (var cue in _cues.Values){cue.NextTime = 0;cue.NextClip=0;}
        }
        private void OnDisable() => ResetSounds();
        private void OnDestroy()
        {
            if (_model != null) { _model.SoundRequested -= RequestSound; _model.SoundReset -= ResetSounds; }
            _environmentAmbience?.Dispose();_environmentWater?.Dispose();
        }
    }
}
