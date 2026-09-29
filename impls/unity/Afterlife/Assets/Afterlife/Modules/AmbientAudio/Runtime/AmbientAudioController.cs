using System;
using UnityEngine;

namespace Afterlife.Modules.AmbientAudio
{
    // Day01 Inspector에 직렬화되는 환경음 설정. Runtime 어셈블리에서 제공한다.
    [Serializable]
    public sealed class AmbientAudioSettings
    {
        [Range(0, 1)] public float GrassVolume = .28f;
        [Range(0, 1)] public float BirdVolume = .38f;
        [Min(1)] public float BirdMinimumGap = 9;
        [Min(1)] public float BirdMaximumGap = 23;
    }

    // 음원과 설정만 주입받는다. Day01/바람/조명의 구체 구현을 참조하지 않는다.
    public sealed class AmbientAudioController : IDisposable
    {
        private readonly AmbientAudioSettings _settings;
        private readonly AudioSource _grass, _bird;
        private readonly AudioClip[] _grassClips;
        private readonly System.Random _random = new System.Random();
        private readonly AmbientTimeline _timeline;
        private float _grassBlend, _grassCooldown, _birdGain = 1;
        private int _lastGrassIndex = -1;
        private bool _disposed;
        private readonly bool _ownsSources;

        public AmbientAudioController(Transform parent, AmbientAudioSettings settings, AudioClip[] grass, AudioClip bird)
            : this(Make(parent, "AmbientGrass", null, false), Make(parent, "AmbientBird", bird, false), settings, grass, bird, true) { }

        public AmbientAudioController(AudioSource grassSource, AudioSource birdSource, AmbientAudioSettings settings, AudioClip[] grass, AudioClip bird)
            : this(grassSource, birdSource, settings, grass, bird, false) { }

        private AmbientAudioController(AudioSource grassSource, AudioSource birdSource, AmbientAudioSettings settings, AudioClip[] grass, AudioClip bird, bool ownsSources)
        {
            _grass = grassSource ?? throw new ArgumentNullException(nameof(grassSource));
            _bird = birdSource ?? throw new ArgumentNullException(nameof(birdSource));
            _ownsSources = ownsSources;
            _settings = settings ?? new AmbientAudioSettings();
            var validGrass = new System.Collections.Generic.List<AudioClip>();
            if (grass != null) foreach (var clip in grass)
                if (clip != null && !validGrass.Contains(clip)) validGrass.Add(clip);
            _grassClips = validGrass.ToArray();
            if (_ownsSources) { Configure(_grass, null, false); Configure(_bird, bird, false); }
            else _bird.clip = bird;
            _timeline = new AmbientTimeline(_random);
            _timeline.BirdRequested += PlayBird;
            if (_grassClips.Length == 0 || bird == null)
                Debug.LogWarning("Afterlife: an ambience clip is missing. Check Resources/AmbientSounds import.", _grass);
        }
        private static void Configure(AudioSource source, AudioClip clip, bool loop)
        {
            source.playOnAwake = false; source.spatialBlend = 0; source.volume = 0;
            source.loop = loop; source.priority = loop ? 180 : 160; source.clip = clip;
        }
        private static AudioSource Make(Transform parent, string name, AudioClip clip, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.volume = 0;
            source.loop = loop;
            source.priority = loop ? 180 : 160;
            source.clip = clip;
            return source;
        }
        public void Tick(float delta, bool active, bool daylight, bool brushingGrass)
        {
            if (_disposed) return;
            _timeline.Tick(delta, active, daylight, _bird.isPlaying, _settings.BirdMinimumGap, _settings.BirdMaximumGap);
            float blend = Mathf.SmoothStep(0, 1, _timeline.Blend);
            bool brushing = active && brushingGrass;
            _grassCooldown = Mathf.Max(0, _grassCooldown - Mathf.Max(0, delta));
            // 정지 시 새 소리만 막고 짧은 클립의 자연스러운 여운은 보존한다.
            if (brushing && !_grass.isPlaying && _grassCooldown <= 0) PlayGrass();
            _grassBlend = Mathf.MoveTowards(_grassBlend, active ? 1 : 0, Mathf.Max(0, delta) / .12f);
            _grass.volume = _grassBlend * Mathf.Clamp01(_settings.GrassVolume);
            if (!active && _grassBlend <= 0) _grass.Stop();
            _bird.volume = blend * _timeline.BirdBlend * Mathf.Clamp01(_settings.BirdVolume) * _birdGain;
            if (_timeline.BirdBlend <= 0) _bird.Stop();
        }
        private void PlayGrass()
        {
            if (_grassClips.Length == 0) return;
            int index = _random.Next(_grassClips.Length);
            if (_grassClips.Length > 1 && index == _lastGrassIndex)
                index = (index + 1 + _random.Next(_grassClips.Length - 1)) % _grassClips.Length;
            _lastGrassIndex = index;
            _grass.clip = _grassClips[index];
            _grass.pitch = 1;
            _grass.Play();
            _grassCooldown = _grass.clip.length + Range(.06f, .16f);
        }
        private void PlayBird()
        {
            if (_bird.clip == null) return;
            _bird.panStereo = Range(-.55f, .55f);
            _bird.pitch = Range(.97f, 1.03f);
            _birdGain = Range(.8f, 1);
            _bird.Play();
        }
        private float Range(float min, float max) => min + (max - min) * (float)_random.NextDouble();
        public void Reset()
        {
            if (_disposed) return;
            _timeline.Reset();
            _grass.Stop(); _bird.Stop();
            _grass.volume = _bird.volume = 0;
            _grassBlend = 0;
            _grassCooldown = 0;
            _lastGrassIndex = -1;
        }
        public void Dispose()
        {
            if (_disposed) return;
            Reset();
            _disposed = true;
            _timeline.BirdRequested -= PlayBird;
            if (_ownsSources)
            {
                Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_grass.gameObject);
                Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_bird.gameObject);
            }
        }
    }
}
