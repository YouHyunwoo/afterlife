using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    [CreateAssetMenu(menuName = "Afterlife/Threshold/Audio Bank")]
    public sealed class ThresholdAudioBank : ScriptableObject
    {
        public AudioClip CameraWhoosh, WaterEnter, WaterExit, WaterLoop;
        public AudioClip TitleMusic, PlayingMusic, GameOverMusic, ClearMusic;
        public AudioClip NightMusic;
        public TextAsset Credits;
        [Range(0, 1)] public float MusicVolume = .32f;
        [Range(0, 1)] public float EffectVolume = .65f;
        [Range(0, 1)] public float WaterVolume = .13f;
        [Range(.5f, 1.5f)] public float WaterPlaybackSpeed = .8f;
        [Min(.1f)] public float MusicCrossfadeSeconds = 1.6f;
        public AudioClip Music(ThresholdScreen screen, bool isNight = false)
        {
            switch (screen)
            {
                case ThresholdScreen.Playing: return isNight && NightMusic != null ? NightMusic : PlayingMusic;
                case ThresholdScreen.GameOver: return GameOverMusic;
                case ThresholdScreen.Clear: return ClearMusic;
                default: return TitleMusic;
            }
        }
    }
}
