using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Local screen audio; owns its sources, never a global music singleton.</summary>
    public sealed class ThresholdSoundscape : ThresholdPart
    {
        public ThresholdAudioBank Bank;
        [Tooltip("Preview time of day; a gameplay day/night adapter can call SetNight.")]
        public bool IsNight;
        public void SetNight(bool value) => IsNight = value;
        public AudioSource Motion, WaterTransitions, WaterBed, MusicA, MusicB;
        private AudioClip _musicClip;
        private int _musicSlot;
        private readonly float[] _weights = new float[2];
        private bool _moving, _modal, _enteredWater, _exitedWater;
        private float _waterGain;
        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Application.isPlaying || Bank == null) return;
            _moving = _modal = _enteredWater = _exitedWater = false;
            _musicClip = null; _musicSlot = 0; _weights[0] = _weights[1] = 0; _waterGain = 0;
            foreach (var source in new[] { Motion, WaterTransitions, WaterBed, MusicA, MusicB })
                if (source != null) { source.playOnAwake = false; source.spatialBlend = 0; source.volume = 0; }
            if (WaterBed != null && Bank.WaterLoop != null)
            { WaterBed.pitch = Mathf.Clamp(Bank.WaterPlaybackSpeed, .5f, 1.5f); WaterBed.clip = Bank.WaterLoop; WaterBed.loop = true; WaterBed.Play(); }
        }
        private void LateUpdate()
        {
            if (Bank == null || Host == null || Host.Session == null) return;
            var state = Host.Session;
            float delta = Time.unscaledDeltaTime;
            float waterSpeed = Mathf.Clamp(Bank.WaterPlaybackSpeed, .5f, 1.5f);
            if (WaterTransitions != null) WaterTransitions.pitch = waterSpeed;
            if (WaterBed != null) WaterBed.pitch = waterSpeed;
            // Keep the existing 0.13 default slider setting as unity gain for this sample mix.
            float master = Mathf.Clamp(Host.AmbienceVolume / .13f, 0, 2);
            if (Motion != null) Motion.volume = Bank.EffectVolume * master;
            if (WaterTransitions != null) WaterTransitions.volume = Bank.EffectVolume * master * .7f;
            bool portal = state.From == ThresholdScreen.Lobby && state.To == ThresholdScreen.Playing;
            if (state.IsMoving && !_moving)
            {
                _enteredWater = _exitedWater = false;
                if (state.IsCameraTravel || portal)
                {
                    if (Motion != null && Bank.CameraWhoosh != null) { Motion.pitch = portal ? 1.25f : 1; Motion.PlayOneShot(Bank.CameraWhoosh); }
                }
                if (!state.IsCameraTravel && !portal) { PlayWater(true); _enteredWater = true; }
            }
            if (state.IsMoving && !state.IsCameraTravel)
            {
                float entry = Host.ReducedMotion ? 0 : ThresholdPresentationSystem.PortalArrival;
                if (portal && !_enteredWater && state.Progress >= entry) { PlayWater(true); _enteredWater = true; }
                if (!state.IsResultEntry && !_exitedWater && state.Progress >= .53f) { PlayWater(false); _exitedWater = true; }
            }
            if (state.ModalOpen != _modal) { PlayWater(state.ModalOpen); _modal = state.ModalOpen; }
            _moving = state.IsMoving;

            var screen = state.Presented;
            var clip = Bank.Music(screen, IsNight);
            if (clip != _musicClip)
            {
                _musicClip = clip; _musicSlot = 1 - _musicSlot;
                var incoming = _musicSlot == 0 ? MusicA : MusicB;
                if (incoming != null)
                { incoming.Stop(); incoming.clip = clip; incoming.loop = true; incoming.volume = 0; _weights[_musicSlot] = 0; if (clip != null) incoming.Play(); }
            }
            float duck = screen == ThresholdScreen.Lobby ? .52f : 1;
            duck *= Mathf.Lerp(1, .5f, Host.ModalAmount);
            if (state.IsMoving && !state.IsCameraTravel) duck *= .7f;
            for (int i = 0; i < 2; i++)
            {
                var source = i == 0 ? MusicA : MusicB;
                if (source == null) continue;
                _weights[i] = Mathf.MoveTowards(_weights[i], i == _musicSlot ? 1 : 0, delta / Mathf.Max(.1f, Bank.MusicCrossfadeSeconds));
                source.volume = _weights[i] * Bank.MusicVolume * master * duck;
                if (i != _musicSlot && _weights[i] == 0 && source.isPlaying) source.Stop();
            }
            float presence = Mathf.Max(Host.ModalAmount, ThresholdSession.IsResult(screen) ? 1 : 0);
            if (state.IsMoving && !state.IsCameraTravel) presence = Mathf.Max(presence, 1.7f);
            _waterGain = Mathf.MoveTowards(_waterGain, presence, delta * 2.5f);
            if (WaterBed != null) WaterBed.volume = _waterGain * Bank.WaterVolume * master;
        }
        private void PlayWater(bool entering)
        {
            if (WaterTransitions == null) return;
            var clip = entering ? Bank.WaterEnter : Bank.WaterExit;
            if (clip != null) WaterTransitions.PlayOneShot(clip);
        }
        protected override void OnDisable()
        {
            foreach (var source in new[] { Motion, WaterTransitions, WaterBed, MusicA, MusicB })
                if (source != null) { source.Stop(); source.clip = null; }
            _musicClip = null;
            base.OnDisable();
        }
    }
}
