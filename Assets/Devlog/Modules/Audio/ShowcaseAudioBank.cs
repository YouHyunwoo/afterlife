using UnityEngine;

namespace Afterlife.Devlog.Audio
{
    [CreateAssetMenu(menuName="Devlog/Audio Bank",fileName="ShowcaseAudioBank")]
    public sealed class ShowcaseAudioBank : ScriptableObject
    {
        public AudioClip WoodImpact;
        public AudioClip WoodFall;
        public AudioClip[] StoneImpacts;
        public AudioClip StoneBreak;
        public AudioClip WaterRipple;
        public AudioClip[] WaterRippleVariants;
        [Range(0,1)] public float WaterRippleVolume=.525f;
        [Range(0,1)] public float Volume=.65f;
        [Range(0,.2f)] public float WoodPitchVariation=.07f;
    }
}
