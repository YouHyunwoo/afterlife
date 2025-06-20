namespace Afterlife.UI
{
    public abstract class Screen : View
    {
        public void Localize() => OnLocalizationChanged();

        protected virtual void OnLocalizationChanged() {}
    }
}