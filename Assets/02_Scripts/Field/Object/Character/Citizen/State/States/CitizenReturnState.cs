namespace Afterlife.Dev.Field
{
    public class CitizenReturnState : CitizenState
    {
        private Building _nearestBuilding;

        public CitizenReturnState(string stateId) : base(stateId) { }

        protected override void OnEnter(object[] args)
        {
            if (model.BuildingLocator.FindNearest(model.Position, BuildingType.House, out _nearestBuilding))
            {
                model.Movement.Destination = _nearestBuilding.Position;
            }
            else
            {
                model.DropHoldings();
                Transit("idle");
            }
        }

        protected override void OnUpdate()
        {
            if (model.Collision.Entered == _nearestBuilding)
            {
                model.Movement.ShouldStop = true;
                model.ReturnHoldings();
                Transit("idle");
            }
        }

        protected override void OnExit(object[] args)
        {
            _nearestBuilding = null;
        }
    }
}
