using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    public enum ThresholdResultBackdrop { KeepWorld, Blackout }
    [CreateAssetMenu(menuName = "Afterlife/Threshold/Profile")]
    public sealed class ThresholdProfile : ScriptableObject
    {
        [Range(.2f, 1f)] public float CameraTravelSeconds = .95f;
        [Min(.5f)] public float WorldTransitionSeconds = 2.4f;
        [Min(.2f)] public float ResultTransitionSeconds = .8f;
        public ThresholdResultBackdrop ResultBackdrop = ThresholdResultBackdrop.KeepWorld;
        [Min(.1f)] public float ModalFadeSeconds = .45f;
        [Range(0, 1)] public float AmbienceVolume = .13f;
        public bool ReducedMotion;
    }
}
