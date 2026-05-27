namespace Afterlife.Dev.Game
{
    public struct HarvestTargetComponent
    {
        public int ResourceEntityId; // 채취할 대상의 Entity ID
        public bool IsHarvesting; // 다가가서 채취 중인지 여부
    }
}
