using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>ThresholdLab modal fade and water audio, independent of any particular UI surface.</summary>
    [DisallowMultipleComponent]
    public sealed class ThresholdModalEffect : MonoBehaviour
    {
        [SerializeField] private ThresholdProfile _profile;
        [SerializeField] private ThresholdAudioBank _bank;
        private AudioSource _transitions;
        private AudioSource _waterBed;
        private float _amount;
        private float _waterGain;
        private bool _open;

        public float Amount => _amount;
        public bool IsClosed => !_open && _amount <= 0;

        public void Configure(ThresholdProfile profile, ThresholdAudioBank bank)
        {
            _profile = profile;
            _bank = bank;
        }

        public void SetOpen(bool open, bool restart = false)
        {
            if (_open == open && !restart) return;
            _open = open;
            if (restart) _amount = 0;
            if (!Application.isPlaying || _bank == null) return;
            EnsureSources();
            float master = _profile != null ? Mathf.Clamp(_profile.AmbienceVolume / .13f, 0, 2) : 1;
            _transitions.pitch = Mathf.Clamp(_bank.WaterPlaybackSpeed, .5f, 1.5f);
            _transitions.volume = _bank.EffectVolume * master * .7f;
            var clip = open ? _bank.WaterEnter : _bank.WaterExit;
            if (clip != null) _transitions.PlayOneShot(clip);
            if (open && _bank.WaterLoop != null && !_waterBed.isPlaying)
            {
                _waterBed.clip = _bank.WaterLoop;
                _waterBed.loop = true;
                _waterBed.pitch = Mathf.Clamp(_bank.WaterPlaybackSpeed, .5f, 1.5f);
                _waterBed.Play();
            }
        }

        private void LateUpdate()
        {
            float duration = _profile != null ? Mathf.Max(.1f, _profile.ModalFadeSeconds) : .45f;
            _amount = Mathf.MoveTowards(_amount, _open ? 1 : 0, Time.unscaledDeltaTime / duration);
            _waterGain = Mathf.MoveTowards(_waterGain, _amount, Time.unscaledDeltaTime * 2.5f);
            if (_waterBed == null || _bank == null) return;
            float master = _profile != null ? Mathf.Clamp(_profile.AmbienceVolume / .13f, 0, 2) : 1;
            _waterBed.volume = _waterGain * _bank.WaterVolume * master;
            if (!_open && _waterGain <= 0 && _waterBed.isPlaying) _waterBed.Stop();
        }

        private void EnsureSources()
        {
            if (_transitions != null && _waterBed != null) return;
            var audio = new GameObject("ThresholdModalAudio");
            audio.transform.SetParent(transform, false);
            _transitions = audio.AddComponent<AudioSource>();
            _waterBed = audio.AddComponent<AudioSource>();
            _transitions.playOnAwake = _waterBed.playOnAwake = false;
            _transitions.spatialBlend = _waterBed.spatialBlend = 0;
            _waterBed.volume = 0;
        }

        public void ResetImmediate()
        {
            _open = false;
            _amount = _waterGain = 0;
            if (_transitions != null) _transitions.Stop();
            if (_waterBed != null) _waterBed.Stop();
        }

        private void OnDisable() => ResetImmediate();
    }
}
