using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ThresholdPage : ThresholdPart
    {
        public ThresholdScreen Screen;
        public bool IsModal;
        public CanvasGroup Group;
        private void Reset() => Group = GetComponent<CanvasGroup>();
    }
}
