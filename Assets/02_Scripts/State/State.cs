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

        public void Enter(object[] args = null)
            => OnEnter(args);

        public void Update()
            => OnUpdate();

        public void Exit(object[] args = null)
            => OnExit(args);

        public virtual void Request<T>(T args = default) where T : StateRequestParam { }

        protected virtual void OnInitialize() { }
        protected virtual void OnEnter(object[] args) { }
        protected virtual void OnUpdate() { }
        protected virtual void OnExit(object[] args) { }

        protected void Transit(State nextState, object[] exitArgs = null, object[] enterArgs = null)
            => stateMachine.Transit(nextState, exitArgs, enterArgs);
        protected void Transit(string stateId, object[] exitArgs = null, object[] enterArgs = null)
            => stateMachine.Transit(stateId, exitArgs, enterArgs);
    }
}