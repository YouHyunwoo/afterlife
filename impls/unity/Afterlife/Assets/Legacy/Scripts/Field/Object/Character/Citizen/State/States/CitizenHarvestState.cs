namespace Afterlife.Dev.Field
{
    public class CitizenHarvestState : CitizenState
    {
        private Resource _target;

        public CitizenHarvestState(string stateId) : base(stateId) { }

        protected override void OnEnter(object[] args)
        {
            _target = (Resource)args[0];
            model.Movement.Destination = _target.Position;
            model.Collision.NeedsRefresh = true;
            _target.OnHarvested += HandleHarvested;
        }

        private void HandleHarvested(HarvestResultInfo harvestResultInfo, Resource resource, object sender)
        {
            Transit("return");
        }

        protected override void OnUpdate()
        {
            if (model.Collision.Entered == _target && !_target.IsHarvested)
            {
                model.Movement.ShouldStop = true;
                _target.AddWorker(model);
            }
        }

        protected override void OnExit(object[] args)
        {
            _target.RemoveWorker(model);
            _target.OnHarvested -= HandleHarvested;
        }
    }
}