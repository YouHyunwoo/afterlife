namespace Afterlife.Dev.Game
{
    public struct ResourceComponent
    {
        public int Woods;
        public int Stones;
        public HarvestState State;
        public float HarvestRate;
        public float HarvestSpeed;
        public int WorkerCount;

        public readonly bool IsHarvested
            => State == HarvestState.Harvested;
    }
}