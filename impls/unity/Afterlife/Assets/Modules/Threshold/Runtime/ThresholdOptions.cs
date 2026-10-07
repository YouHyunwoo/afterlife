using UnityEngine.UI;

namespace Afterlife.Modules.Threshold.Unity
{
    public sealed class ThresholdOptions : ThresholdPart
    {
        public Slider Volume;
        public Toggle ReducedMotion;
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Volume != null) { Volume.onValueChanged.AddListener(ChangeVolume); Volume.SetValueWithoutNotify(Host != null ? Host.AmbienceVolume : .13f); }
            if (ReducedMotion != null) { ReducedMotion.onValueChanged.AddListener(ChangeMotion); ReducedMotion.SetIsOnWithoutNotify(Host != null && Host.ReducedMotion); }
        }
        private void ChangeVolume(float value) { if (Host != null) Host.SetAmbienceVolume(value); }
        private void ChangeMotion(bool value) { if (Host != null) Host.SetReducedMotion(value); }
        protected override void OnDisable()
        {
            if (Volume != null) Volume.onValueChanged.RemoveListener(ChangeVolume);
            if (ReducedMotion != null) ReducedMotion.onValueChanged.RemoveListener(ChangeMotion);
            base.OnDisable();
        }
    }
}
