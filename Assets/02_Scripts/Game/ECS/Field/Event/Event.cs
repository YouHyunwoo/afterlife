namespace Afterlife.Dev.Game
{
    public abstract class Event
    {
        public float TriggerTime { get; }
        public bool IsComplete { get; private set; }

        protected Event(float triggerTime) => TriggerTime = triggerTime;

        protected virtual void OnStart() { }
        protected virtual void OnUpdate(float deltaTime, float currentTime) { }
        protected virtual void OnEnd() { }

        internal void Start() => OnStart();
        public void Update(float deltaTime, float currentTime) => OnUpdate(deltaTime, currentTime);

        protected void End()
        {
            if (IsComplete) return;
            IsComplete = true;
            OnEnd();
        }
    }
}
