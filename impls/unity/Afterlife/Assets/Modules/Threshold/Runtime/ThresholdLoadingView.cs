using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Full-screen cover while a game-specific adapter prepares the next scene.</summary>
    [DisallowMultipleComponent]
    public sealed class ThresholdLoadingView : MonoBehaviour
    {
        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
