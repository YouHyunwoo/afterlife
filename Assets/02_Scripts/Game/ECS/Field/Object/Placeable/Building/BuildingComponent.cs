namespace Afterlife.Dev.Game
{
    public struct BuildingComponent
    {
        public BuildingType Type;
        public BuildingMode Mode;
        public BuildingState State;
        public float BuildRate;
        public float BuildSpeed;
        public float TownZoneRadius;
        public int WorkerCount;

        public readonly bool IsBuilt => State == BuildingState.Built;
    }
}
