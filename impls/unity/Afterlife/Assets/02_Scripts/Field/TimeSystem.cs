namespace Afterlife.Dev.Field
{
    public class TimeSystem : Moonstone.Ore.Local.System
    {
        public float ElapsedTime { get; private set; }
        public float TimeScale { get; set; } = 1f;

        protected override void OnSetUp() => ElapsedTime = 0f;
        protected override void OnTearDown() => ElapsedTime = 0f;

        private void Update()
        {
            ElapsedTime += UnityEngine.Time.deltaTime * TimeScale;
        }
    }
}