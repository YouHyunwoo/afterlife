using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Threshold.Unity
{
    [RequireComponent(typeof(Button))]
    public sealed class ThresholdActionButton : ThresholdPart
    {
        public ThresholdAction Action;
        protected override void OnEnable() { base.OnEnable(); GetComponent<Button>().onClick.AddListener(Invoke); }
        protected override void OnDisable() { GetComponent<Button>().onClick.RemoveListener(Invoke); base.OnDisable(); }
        private void Invoke() { if (Host != null) Host.Dispatch(Action); }
    }
}
