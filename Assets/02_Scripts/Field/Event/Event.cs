namespace Afterlife.Dev.Field
{
    public abstract class Event
    {
        public float TriggerTime { get; }
        public bool IsComplete { get; private set; }

        protected Event(float triggerTime)
        {
            TriggerTime = triggerTime;
        }

        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnEnd() { }

        internal void Start() => OnStart();

        public void Update() => OnUpdate();

        protected void End()
        {
            if (IsComplete) return;
            IsComplete = true;
            OnEnd();
        }
    }
}
