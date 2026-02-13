using UnityEngine;

namespace Afterlife.UI
{
    public abstract class Screen : View
    {
        [HideInInspector] public Controller Controller;

        public void ObtainController() => Controller = GetComponent<Controller>();

        public void Localize() => OnLocalizationChanged();

        protected virtual void OnLocalizationChanged() { }
    }
}