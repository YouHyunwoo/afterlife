using System.Collections.Generic;

namespace Afterlife.Dev.State
{
    public class StateMachine
    {
        protected State[] states;
        protected Dictionary<string, State> stateMap;
        protected StateContext context;
        protected State currentState;

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

        public void Run(State initialState = null)
        {
            if (initialState == null)
                Transit(states[0]);
            else
                Transit(initialState);
        }

        public void Update()
            => currentState?.Update();

        public void Transit(State nextState)
        {
            currentState?.Exit();
            currentState = nextState;
            currentState?.Enter();
        }

        public void Transit(string stateId)
            => Transit(stateMap[stateId]);

        public void Request<T>(T args) where T : StateRequestParam
            => currentState?.Request(args);
    }
}