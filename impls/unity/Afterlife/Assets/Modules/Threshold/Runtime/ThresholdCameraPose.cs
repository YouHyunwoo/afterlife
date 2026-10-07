using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    public sealed class ThresholdCameraPose : ThresholdPart
    {
        public ThresholdScreen Screen;
        [Range(20, 90)] public float FieldOfView = 48;
    }
}
