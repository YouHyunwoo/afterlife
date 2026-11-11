namespace Afterlife.Dev.Field
{
    public class CitizenBuildState : CitizenState
    {
        private Building _target;

        public CitizenBuildState(string stateId) : base(stateId) { }

        protected override void OnEnter(object[] args)
        {
            _target = (Building)args[0];
            model.Movement.Destination = _target.Position;
            model.Collision.NeedsRefresh = true;
            _target.OnBuilt += HandleBuilt;
        }

        protected override void OnUpdate()
        {
            if (model.Collision.Entered == _target && !_target.IsBuilt)
            {
                model.Movement.ShouldStop = true;
                _target.AddWorker(model);
            }
        }

        protected override void OnExit(object[] args)
        {
            _target.RemoveWorker(model);
            _target.OnBuilt -= HandleBuilt;
        }

        private void HandleBuilt(Building building, object sender)
        {
            Transit("idle");
        }
    }
}