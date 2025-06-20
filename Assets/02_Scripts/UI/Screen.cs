using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI
{
    public abstract class Screen : View
    {
        [SerializeField] Image transitionImage;

        public void Localize() => OnLocalizationChanged();

        protected virtual void OnLocalizationChanged() { }
    }
}