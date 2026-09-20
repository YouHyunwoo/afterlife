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
        private readonly AudioSource[] _voices = new AudioSource[12];
        private VillageModel _model;
        private PrototypeSettings _settings;
        private Camera _camera;
        private AudioListener _ownedListener;
        private int _feedbackVoice;

        public void Initialize(VillageModel model, PrototypeSettings settings, Camera camera)
        {
            if (_model != null) return;
            _model = model; _settings = settings; _camera = camera;
            bool hasListener = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (listener.isActiveAndEnabled) { hasListener = true; break; }
            if (!hasListener) _ownedListener = camera.gameObject.AddComponent<AudioListener>();
            for (int i = 0; i < _voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
                source.dopplerLevel = 0; source.priority = i < 8 ? 160 : 64;
                _voices[i] = source;
            }
            Add(SoundCue.Wood, .75f, .12f, false, "01B", "01C");
            Add(SoundCue.ResourceEmpty, .65f, .20f, false, "03A");
            Add(SoundCue.Gold, .55f, .35f, false, "04A");
            Add(SoundCue.Place, .75f, .12f, true, "05A");
            Add(SoundCue.Work, .65f, .14f, false, "06B");
            Add(SoundCue.Complete, .70f, .22f, true, "07A");
            Add(SoundCue.Hit, .70f, .08f, false, "09C");
            Add(SoundCue.Magic, .70f, .12f, false, "12B");
            Add(SoundCue.Kill, .65f, .16f, false, "14A");
            Add(SoundCue.Critical, .80f, .15f, false, "15A");
            Add(SoundCue.BuildingHit, .20f, .75f, false, "16B");
            Add(SoundCue.Collapse, .80f, .25f, true, "16B");
            Add(SoundCue.Chest, .80f, .25f, true, "17B");
            Add(SoundCue.Relic, .80f, .25f, true, "18A");
            Add(SoundCue.Select, .60f, .06f, true, "19A");
            Add(SoundCue.Cancel, .65f, .10f, true, "20A");
            Add(SoundCue.Open, .75f, .12f, true, "21C");
            Add(SoundCue.Error, .75f, .20f, true, "22A");
            Add(SoundCue.Warning, .65f, .50f, true, "23C");
            Add(SoundCue.Day, .70f, .50f, true, "24C");
            Add(SoundCue.Result, .80f, .50f, true, "25B");
            Add(SoundCue.Rebirth, .70f, .50f, true, "26C");
            InitializeEnvironmentAudio();
            _model.SoundRequested += RequestSound;
            _model.SoundReset += ResetSounds;
            RequestSound(SoundCue.Day);
        }

        private void Add(SoundCue key, float volume, float cooldown, bool important, params string[] ids)
        {
            var clips = new List<AudioClip>();
            foreach (string id in ids)
            {
                var clip = Resources.Load<AudioClip>("AfterlifePrototype/Audio/" + id);
                if (clip != null) clips.Add(clip);
                else Debug.LogWarning("Afterlife 효과음 누락: " + id, this);
            }
            _cues.Add(key, new Cue { Clips = clips.ToArray(), Volume = volume, Cooldown = cooldown, Important = important });
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
            if (_ownedAudioBank != null) Destroy(_ownedAudioBank);
            if (_ownedListener != null) Destroy(_ownedListener);
            foreach (var source in _voices) if (source != null) Destroy(source);
        }
    }
}
