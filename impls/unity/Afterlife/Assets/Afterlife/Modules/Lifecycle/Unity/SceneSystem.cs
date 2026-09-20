namespace Afterlife.Modules.Lifecycle
{
    // 씬 수명과 별도로 게임 시스템의 활성 구간을 제어한다.
    public abstract class SceneSystem : SceneBehaviour
    {
        public void SetUp() { Initialize(); OnSetUp(); }
        public void TearDown() => OnTearDown();
        protected virtual void OnSetUp() { }
        protected virtual void OnTearDown() { }
    }
}
