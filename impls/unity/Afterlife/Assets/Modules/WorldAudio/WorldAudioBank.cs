using UnityEngine;

namespace Afterlife.Modules.WorldAudio
{
    [CreateAssetMenu(menuName="Afterlife/World Audio/Bank",fileName="WorldAudioBank")]
    public sealed class WorldAudioBank : ScriptableObject
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
