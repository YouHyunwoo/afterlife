using TMPro;
using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Apply the ThresholdLab frame to an existing modal without changing its content or callbacks.</summary>
    [DisallowMultipleComponent]
    public sealed class ThresholdModalFrame : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Color _surface = new Color(.018f, .027f, .035f, .97f);
        [SerializeField] private Color _frame = new Color(.86f, .72f, .46f, .85f);
        [SerializeField] private Color _title = new Color(.87f, .91f, .93f);

        public void Configure(TMP_FontAsset font) => _font = font;

        private void OnEnable() => Apply();

        public void Apply()
        {
            foreach (var image in GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.name == "Background Image" || image.name == "Header") image.color = _surface;
                else if (image.name == "Frame Image" || image.name == "Divider") image.color = _frame;
            }
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            {
                if (_font != null && label.font == null) label.font = _font;
                if (label.name == "Title Text") label.color = _title;
            }
        }
    }
}
