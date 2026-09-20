namespace Afterlife.Modules.Lifecycle
{
    public abstract class SceneVisible : Moonstone.Core.Visible
    {
        protected virtual void Awake() => Initialize();
        protected virtual void OnDestroy() => Dispose();
    }
}
