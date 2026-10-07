using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Original synthesized water-like bed, no external recording or license dependency.</summary>
    [RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter))]
    public sealed class ThresholdAmbience : ThresholdPart
    {
        private AudioClip _clip;
        private AudioSource _source;
        private AudioLowPassFilter _filter;
        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Application.isPlaying) return;
            _source = GetComponent<AudioSource>(); _filter = GetComponent<AudioLowPassFilter>();
            const int rate = 22050, length = rate * 8;
            var samples = new float[length];
            // Integer cycles make the loop continuous. Several low beating tones imply submerged pressure.
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / rate;
                samples[i] = .18f * Mathf.Sin(t * Mathf.PI * 2 * 63) + .11f * Mathf.Sin(t * Mathf.PI * 2 * 64.125f)
                    + .04f * Mathf.Sin(t * Mathf.PI * 2 * 187) * (1 + .6f * Mathf.Sin(t * Mathf.PI * .75f))
                    + .017f * Mathf.Sin(t * Mathf.PI * 2 * 521) * Mathf.Pow(.5f + .5f * Mathf.Sin(t * Mathf.PI * 2 * .625f), 8);
            }
            _clip = AudioClip.Create("Threshold Submerged Ambience", length, 1, rate, false); _clip.SetData(samples, 0);
            _source.clip = _clip; _source.loop = true; _source.spatialBlend = 0; _source.volume = 0; _source.Play();
        }
        private void Update()
        {
            if (Host == null || Host.Session == null || _source == null) return;
            var state = Host.Session;
            float clear = state.Presented == ThresholdScreen.Playing || state.Presented == ThresholdScreen.Clear ? 1 : 0;
            float volume = Host.AmbienceVolume;
            _source.volume = Mathf.MoveTowards(_source.volume, volume * (clear > 0 ? .3f : 1), Time.unscaledDeltaTime * .15f);
            _filter.cutoffFrequency = Mathf.Lerp(_filter.cutoffFrequency, Mathf.Lerp(350, 3200, clear * (1 - Host.ModalAmount)), Time.unscaledDeltaTime * 3);
        }
        protected override void OnDisable()
        {
            if (_source != null) { _source.Stop(); _source.clip = null; }
            if (_clip != null) Destroy(_clip);
            base.OnDisable();
        }
    }
}
