namespace Afterlife.Modules.Lifecycle
{
    // 생성되는 프리팹에도 Moonstone Core 수명주기를 적용한다.
    public abstract class SceneBehaviour : Moonstone.Core.LifecycleBehaviour
    {
        protected virtual void Awake() => Initialize();
        protected virtual void OnDestroy() => Dispose();
    }
}
