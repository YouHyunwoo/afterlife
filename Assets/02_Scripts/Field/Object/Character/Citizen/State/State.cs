namespace Afterlife.Dev.Field
{
    public class CitizenState : State.State
    {
        protected CitizenStateContext context;
        protected Citizen model;

        public CitizenState(string stateId) : base(stateId)
        {
        }

        protected override void OnInitialize()
        {
            context = stateContext as CitizenStateContext;
            model = context.Model;
        }
    }
}