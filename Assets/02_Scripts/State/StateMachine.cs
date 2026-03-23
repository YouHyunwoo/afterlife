using System.Collections.Generic;

namespace Afterlife.Dev.State
{
    public class StateMachine
    {
        protected State[] states;
        protected Dictionary<string, State> stateMap;
        protected StateContext context;
        protected State currentState;

        public State CurrentState => currentState;

        public void Initialize(State[] states, StateContext context)
        {
            this.states = states;
            stateMap = new();
            foreach (var state in states)
                stateMap[state.Id] = state;
            this.context = context;
            currentState = null;
            foreach (var state in states)
                state.Initialize(context, this);
        }

        public void Run(State initialState = null, object[] enterArgs = null)
        {
            if (initialState == null)
                Transit(states[0], null, enterArgs);
            else
                Transit(initialState, null, enterArgs);
        }

        public void Update()
            => currentState?.Update();

        public void Transit(State nextState, object[] exitArgs = null, object[] enterArgs = null)
        {
            currentState?.Exit(exitArgs);
            currentState = nextState;
            currentState?.Enter(enterArgs);
        }

        public void Transit(string stateId, object[] exitArgs = null, object[] enterArgs = null)
            => Transit(stateMap[stateId], exitArgs, enterArgs);

        public void Request<T>(T args) where T : StateRequestParam
            => currentState?.Request(args);
    }
}