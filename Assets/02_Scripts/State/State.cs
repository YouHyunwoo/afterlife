namespace Afterlife.Dev.State
{
    public class State
    {
        public readonly string Id;
        protected StateMachine stateMachine;
        protected StateContext stateContext;

        public State(string stateId) => Id = stateId;

        public void Initialize(StateContext stateContext, StateMachine stateMachine)
        {
            this.stateContext = stateContext;
            this.stateMachine = stateMachine;
            OnInitialize();
        }

        public void Enter()
            => OnEnter();

        public void Update()
            => OnUpdate();

        public void Exit()
            => OnExit();

        public virtual void Request<T>(T args = default) where T : StateRequestParam { }

        protected virtual void OnInitialize() { }
        protected virtual void OnEnter() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnExit() { }

        protected void Transit(State nextState)
            => stateMachine.Transit(nextState);
        protected void Transit(string stateId)
            => stateMachine.Transit(stateId);
    }
}