namespace Afterlife.Dev.Field
{
    public class EnemyState : State.State
    {
        protected EnemyStateContext context;
        protected EnemyVisible visible;

        public EnemyState(string stateId) : base(stateId)
        {
        }

        protected override void OnInitialize()
        {
            context = stateContext as EnemyStateContext;
            visible = context.EnemyVisible;
        }
    }
}